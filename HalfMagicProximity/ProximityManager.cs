using System.Text;
using System.Diagnostics;

namespace HalfMagicProximity
{
    public class ProximityManager
    {
        private const string LogSource = "ProximityManager";

        private List<CardData> allCards;

        private const string BatFileName = "hlfproxies.bat";
        private string batPath;
        private const string ProximityJarName = "proximity-0.6.2.jar";
        private string proximityPath;
        private const string DeckFileName = "cards.txt";
        private string deckPath;

        public ProximityManager(List<CardData> allCards)
        {
            this.allCards = allCards ?? throw new ArgumentNullException(nameof(allCards));

            deckPath = Path.Combine(ConfigManager.ProximityDirectory, DeckFileName);
            proximityPath = Path.Combine(ConfigManager.ProximityDirectory, ProximityJarName);
            batPath = Path.Combine(ConfigManager.ProximityDirectory, BatFileName);
        }

        public void Run()
        {
            Logger.Info(LogSource, "Beginning Proximity preparations.");

            GenerateDeckFiles();

            if (VerifyProximityFiles())
            {
                Logger.Info(LogSource, "All necessary proximity files are present. Executing now.");
                ExecuteProximityBatchFile();
            }
            else
            {
                Logger.Error(LogSource, $"Unable to run proximity. You may be able to run it manually using the following:\n - Deck File: {deckPath} \n - Batch File: {batPath}");
            }
        }

        private void GenerateDeckFiles()
        {
            try
            {
                int successfulCards = 0;

                using (FileStream deckStream = File.Create(deckPath))
                {
                    // Add cards to file
                    foreach (CardData card in allCards)
                    {
                        string cardString = GenerateCardString(card);
                        byte[] cardBytes = new UTF8Encoding(true).GetBytes(cardString + Environment.NewLine);

                        // Write card string to the deck file
                        deckStream.Write(cardBytes, 0, cardBytes.Length);
                        successfulCards++;
                    }
                }

                if (File.Exists(deckPath))
                    Logger.Info(LogSource, $"Generated deck file with {successfulCards} at '{deckPath}'.");
                else
                    Logger.Error(LogSource, $"Unable to generate deck file at '{deckPath}'!");
            }
            catch (DirectoryNotFoundException e)
            {
                Logger.Error(LogSource, $"Unable to find Proximity directory '{ConfigManager.ProximityDirectory}'!");
            }
            catch (Exception e)
            {
                Logger.Error(LogSource, $"Error generating proximity deck file: {e.Message}");
            }
        }

        private const string OverrideTemplate = " --override=";
        private string GenerateCardString(CardData card)
        {
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

            Logger.Debug(LogSource, cardString);

            return cardString;
        }

        private bool VerifyProximityFiles()
        {
            // Check for the deck (that we presumably just made)
            if (File.Exists(deckPath))
            {
                Logger.Info(LogSource, $"Deck file '{DeckFileName}' is present.");
            }
            else
            {
                Logger.Error(LogSource, $"Deck file not found: {deckPath}");
                return false;
            }

            // Check for the proximity jar file
            if (File.Exists(proximityPath))
            {
                Logger.Info(LogSource, $"Proximity file '{ProximityJarName}' is present.");
            }
            else
            {
                Logger.Error(LogSource, $"Proximity jar file not found: {proximityPath}");
                return false;
            }

            // Check that the proximity batch file exists
            if (File.Exists(batPath))
            {
                Logger.Info(LogSource, $"Batch file '{BatFileName}' is present.");
            }
            else
            {
                Logger.Warn(LogSource, $"Batch file not found. Recreating now.");

                // If the batch file doesn't exist, try and recreate it since its contents are very light
                CreateBatchFile();

                if (File.Exists(batPath))
                {
                    Logger.Info(LogSource, $"Batch file '{BatFileName}' successfully recreated.");
                }
                else
                {
                    Logger.Error(LogSource, $"Unable to recreate batch file '{BatFileName}': {batPath}");

                    return false;
                }
            }

            return true;
        }

        private void CreateBatchFile()
        {
            try
            {
                using (FileStream batchStream = File.Create(batPath))
                {
                    string templatePath = Path.Combine(ConfigManager.ProximityDirectory, "templates", "hlf.zip").Replace("\\", "\\\\");
                    string batchString = $"java -jar \"{proximityPath}\" --template=\"{templatePath}\" --cards=\"{deckPath.Replace("\\", "\\\\")}\" --art_source=BEST --set_symbol=jmp --use_card_back=true";
                    byte[] batchBytes = new UTF8Encoding(true).GetBytes(batchString);

                    batchStream.Write(batchBytes, 0, batchBytes.Length);
                }
            }
            catch (DirectoryNotFoundException e)
            {
                Logger.Error(LogSource, $"Unable to find Proximity directory '{ConfigManager.ProximityDirectory}'!");
            }
            catch (Exception e)
            {
                Logger.Error(LogSource, $"Error generating proximity batch file: {e.Message}");
            }
        }

        private void ExecuteProximityBatchFile()
        {
            Process proximityProcess = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo(batPath);
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            proximityProcess.StartInfo = startInfo;
            proximityProcess.OutputDataReceived += HandleProximityOutput;
            proximityProcess.Start();
            proximityProcess.BeginOutputReadLine();
            proximityProcess.WaitForExit();
        }

        private void HandleProximityOutput(object sender, DataReceivedEventArgs args)
        {
            if (!string.IsNullOrEmpty(args.Data))
                Logger.Proximity("Proximity", args.Data.Replace(Environment.NewLine, ""));
        }
    }
}
