using System.Diagnostics;
using System.Text;

namespace HalfMagicProximity
{
    /// <summary>
    /// A batch of cards to be run through Proximity
    /// Cards are split into batches to ensure smooth rendering
    /// </summary>
    public class ProximityBatch
    {
        private const string LogSource = "Batch";
        private string namedLogSource => $"{LogSource}: {name}";
        public const int MaxCardCount = 20; // Should be even so both halves of a card end up in the same batch

        private ProximityManager manager;

        private string name;

        private string deckFile => name + "_decklist.txt";
        private string deckPath => Path.Combine(ConfigManager.ProximityDirectory, deckFile);

        private string commandFile => name + "_proximityCommand.bat";
        private string commandPath => Path.Combine(ConfigManager.ProximityDirectory, commandFile);

        private string proximityFile;
        private string proximityPath => Path.Combine(ConfigManager.ProximityDirectory, proximityFile);

        public int CardCount { get; private set; }
        public bool IsFull => CardCount >= MaxCardCount;
        public bool IsBatchFunctional => !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(proximityFile) && CardCount > 0;

        public ProximityBatch(ProximityManager manager, string name, string prox)
        {
            this.manager = manager ?? throw new ArgumentNullException(nameof(manager));

            if (string.IsNullOrEmpty(name))
                Logger.Error(LogSource, $"No batch name provided. Unable to run proximity without a batch name!");
            else
                this.name = name;

            if (string.IsNullOrEmpty(prox))
                Logger.Error(namedLogSource, $"No proximity file provided. Unable to run proximity without the jar file!");
            else
                proximityFile = prox;

            Logger.Trace(namedLogSource, $"Batch {this.name} successfully created.");
        }

        /// <summary>
        /// Initializes a batch and makes sure it's ready to run
        /// </summary>
        private bool Init()
        {
            GenerateDeckFile();
            GenerateCommandFile();

            bool ready = false;
            if (IsBatchFunctional)
                ready = VerifyProximityFiles();

            if (ready)
                Logger.Debug(namedLogSource, $"Fully intialized with {CardCount} cards ({CardCount * 2} faces) to render.");
            else
                Logger.Error(namedLogSource, $"Failed to fully initialize!");

            return ready;
        }

        /// <summary>
        /// Initialize the batch, then run the proximity file
        /// </summary>
        public void Run()
        {
            if (Init())
            {
                Logger.Trace(namedLogSource, $"Beginning render for {name}.");

                Process proximityProcess = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo(commandPath);
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;
                proximityProcess.StartInfo = startInfo;
                proximityProcess.OutputDataReceived += HandleProximityOutput;
                proximityProcess.Start();
                proximityProcess.BeginOutputReadLine();
                proximityProcess.WaitForExit();

                Logger.Trace(namedLogSource, $"Completed render for {name}.");
                if (failedRenderCount > 0)
                    Logger.Warn(namedLogSource, $"Failed to render {failedRenderCount} card{(failedRenderCount == 1 ? "" : "s")}.");
            }
        }

        private int failedRenderCount = 0;
        private void HandleProximityOutput(object sender, DataReceivedEventArgs args)
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                Logger.Proximity("Proximity", args.Data.Replace(Environment.NewLine, ""));

                if (args.Data.ToLower().Contains("failed"))
                {
                    failedRenderCount++;

                    string[] splitArgs = args.Data.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    // Format is 'Severity [Proximity] X/YY ZZZZms Name                 FAILED'
                    if (splitArgs.Length >= 5)
                    {
                        // Name will comprise the fourth through second to last substring
                        string failedCardName = "";
                        for (int i = 4; i < splitArgs.Length - 1; i++)
                            failedCardName += splitArgs[i] + " ";

                        manager.HandleFailedRender(failedCardName.Trim());
                    }
                    else
                        Logger.Error(namedLogSource, $"Unable to determine name for failed card render. Cannot try again!");
                }
            }
        }

        /// <summary>
        /// Verify that everything the batch needs to run is present
        /// </summary>
        private bool VerifyProximityFiles()
        {
            // Check for the deck (that we presumably just made)
            if (File.Exists(deckPath))
            {
                Logger.Trace(namedLogSource, $"Deck file '{deckFile}' is present.");
            }
            else
            {
                Logger.Error(namedLogSource, $"Deck file not found: {deckPath}");
                return false;
            }

            // Check for the proximity jar file
            if (File.Exists(proximityPath))
            {
                Logger.Trace(namedLogSource, $"Proximity file '{proximityFile}' is present.");
            }
            else
            {
                Logger.Error(namedLogSource, $"Proximity jar file not found: {proximityPath}");
                return false;
            }

            // Check for the batch file to run
            if (File.Exists(commandPath))
            {
                Logger.Trace(namedLogSource, $"Batch file '{commandFile}' is present.");
            }
            else
            {
                Logger.Error(namedLogSource, $"Batch file not found: {commandPath}");

                return false;
            }

            return true;
        }

        /// <summary>
        /// Creates the batch file to run this group of proximity cards
        /// </summary>
        private void GenerateCommandFile()
        {
            Logger.Trace(namedLogSource, $"Beginning to generate command file '{commandFile}'.");

            try
            {
                using (FileStream commandStream = File.Create(commandPath))
                {
                    // ARGTODO: Include RAM allocation override (-Xmx8g) after 'java'?
                    string templatePath = Path.Combine(ConfigManager.ProximityDirectory, "templates", "hlf.zip").Replace("\\", "\\\\");
                    string commandString = $"java -jar \"{proximityPath}\" --template=\"{templatePath}\" --cards=\"{deckPath.Replace("\\", "\\\\")}\" --art_source=BEST --set_symbol=jmp --use_card_back=true";
                    byte[] commandBytes = new UTF8Encoding(true).GetBytes(commandString);

                    commandStream.Write(commandBytes, 0, commandBytes.Length);
                }

                Logger.Trace(namedLogSource, $"Command file successfully generated: {commandPath}");
            }
            catch (DirectoryNotFoundException e)
            {
                Logger.Error(namedLogSource, $"Unable to find Proximity directory '{ConfigManager.ProximityDirectory}'!");
            }
            catch (Exception e)
            {
                Logger.Error(namedLogSource, $"Error generating proximity command file: {e.Message}");
            }
        }

        private string deckContents = "";

        /// <summary>
        /// Generates the decklist for this batch
        /// </summary>
        private void GenerateDeckFile()
        {
            Logger.Trace(namedLogSource, $"Beginning to generate decklist file '{deckFile}'.");

            if (string.IsNullOrEmpty(deckContents))
            {
                Logger.Error(namedLogSource, $"Deck contents are empty, unable to generate deck file!");
                return;
            }

            try
            {
                using (FileStream deckStream = File.Create(deckPath))
                {
                    byte[] cardBytes = new UTF8Encoding(true).GetBytes(deckContents);
                    deckStream.Write(cardBytes, 0, cardBytes.Length);
                }

                if (File.Exists(deckPath))
                    Logger.Trace(namedLogSource, $"Deck file containing {CardCount} cards successfully generated at: {deckPath}");
                else
                    Logger.Error(namedLogSource, $"Unable to generate deck file at '{deckPath}'!");
            }
            catch (DirectoryNotFoundException e)
            {
                Logger.Error(namedLogSource, $"Unable to find Proximity directory '{ConfigManager.ProximityDirectory}'!");
            }
            catch (Exception e)
            {
                Logger.Error(namedLogSource, $"Error generating proximity deck file: {e.Message}");
            }
        }

        public void AddCard(CardData card)
        {
            string cardString = GenerateCardString(card);

            if (!string.IsNullOrEmpty(cardString))
            {
                deckContents += cardString + Environment.NewLine;
                CardCount++;

                Logger.Trace(namedLogSource, $"{card.DisplayName} added to batch ({CardCount}/{MaxCardCount}).");
            }
        }

        /// <summary>
        /// Generates the decklist line for a given card
        /// </summary>
        private string GenerateCardString(CardData card)
        {
            if (card.ValidateCard())
            {
                string OverrideTemplate = " --override=";
                string cardString = $"1 {card.Name}";

                // Add rarity override if one was specified in the config settings
                if (ConfigManager.IsProxyRarityOverrided)
                    cardString += OverrideTemplate + "rarity:" + ConfigManager.ProxyRarityOverride;

                // Add color overrides if faces have different colors
                if (card.NeedsColorOverride)
                {
                    cardString += OverrideTemplate + "colors:[\"" + card.Color + "\"]";
                    cardString += OverrideTemplate + "proximity.mtg.color_count:" + card.ColorCount;
                }

                // Add watermark override if the card has one
                if (card.NeedsWatermarkOverride)
                    cardString += OverrideTemplate + "watermark:" + card.Watermark;

                // Add artist override if faces have different artists
                if (card.NeedsArtistOverride)
                    cardString += OverrideTemplate + "artist:\"" + card.Artist + "\"";

                // Back faces and split cards need manually overridden art
                if (card.NeedsArtOverride)
                {
                    string artPath = Path.Combine(ConfigManager.ProximityDirectory, "art", card.ArtFileName).Replace("\\", "/").Replace(" ", "%20");

                    cardString += OverrideTemplate + "image_uris.art_crop:\"\"file:///" + artPath + "\"\"";
                }

                Logger.Trace(namedLogSource, cardString);

                return cardString;
            }

            return "";
        }
    }
}
