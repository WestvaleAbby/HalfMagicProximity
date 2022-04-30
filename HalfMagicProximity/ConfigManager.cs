using System.Reflection;
using System.Text.Json;

namespace HalfMagicProximity
{
    /// <summary>
    /// ConfigManager handles reading in values in the config file and stores them for access throughout the program
    /// </summary>
    static class ConfigManager
    {
        private const string LogSource = "ConfigManager";

        public static string ScryfallPath;
        public static string ProximityDirectory;

        public static string ArtFileExtension;
        private static string defaultArtFileExtension = ".jpg";

        public static string ProxyRarityOverride;
        public static bool IsProxyRarityOverrided => !string.IsNullOrEmpty(ProxyRarityOverride);
        private static string[] validRarities = { "common", "uncommon", "rare", "mythic" };

        public static bool DeleteBadFaces;

        public static List<string> IllegalSetCodes = new List<string>();

        public static bool UseCardSubset;
        public static List<string> CardSubset = new List<string>();

        public static List<ManualArtistOverride> ManualArtistOverrides = new List<ManualArtistOverride>();

        public static bool Valid => !string.IsNullOrEmpty(ScryfallPath) && !string.IsNullOrEmpty(ProximityDirectory);

        private const string CONFIG_FILE_NAME = "hlfconfig.json";

        public static void Init()
        {
            try
            {
                string[] resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();

                string? configPath = resources.FirstOrDefault(x => x.Contains(CONFIG_FILE_NAME)).Replace("HalfMagicProximity.", "");

                if (string.IsNullOrEmpty(configPath)) throw new Exception($"Config File not found!");

                using (Stream configStream = new FileStream(configPath, FileMode.Open, FileAccess.Read))
                using (JsonDocument configDoc = JsonDocument.Parse(configStream))
                {
                    JsonElement configOptions = configDoc.RootElement.GetProperty("hlfOptions");

                    ParseIsTraceEnabled(configOptions);
                    if (!ParseScryfallPath(configOptions)) return;
                    if (!ParseProximityPath(configOptions)) return;
                    ParseArtExtension(configOptions);
                    ParseRarityOverride(configOptions);
                    ParseProxyCleanup(configOptions);
                    ParseIllegalSetCodes(configOptions);
                    ParseCardSubset(configOptions);
                    ParseManualArtistOverrides(configOptions);
                }
            }
            catch (FileNotFoundException e)
            {
                Logger.Error(LogSource, $"Config file not found: {e.Message}");
            }
            catch (JsonException e)
            {
                Logger.Error(LogSource, $"Config JSON Error: {e.Message}");
            }
            catch (Exception e)
            {
                Logger.Error(LogSource, $"Config Error: {e.Message}");
            }
        }

        /// <summary>
        /// Determine whether trace logs are displayed
        /// </summary>
        private static void ParseIsTraceEnabled(JsonElement configOptions)
        {
            Logger.IsTraceEnabled = configOptions.GetProperty("IsTraceEnabled").GetBoolean();
            if (Logger.IsTraceEnabled)
                Logger.Debug(LogSource, $"Trace messages are enabled.");
            else
                Logger.Debug(LogSource, $"Trace messages are disabled.");
        }

        /// <summary>
        /// Try to find path for Scryfall JSON
        /// </summary>
        private static bool ParseScryfallPath(JsonElement configOptions)
        {
            // Find path to scryfall json
            string scryfallString = configOptions.GetProperty("ScryfallPath").GetString();
            bool validScryfallJson = true;

            //Check whether the specified scryfall json works
            if (string.IsNullOrEmpty(scryfallString))
            {
                Logger.Debug(LogSource, $"Path to Scryfall JSON not supplied.");
                validScryfallJson = false;
            }
            else if (!File.Exists(scryfallString))
            {
                Logger.Warn(LogSource, $"Scryfall JSON not found at '{scryfallString}'.");
                validScryfallJson = false;
            }
            else if (File.Exists(scryfallString) && !IsScryfallJSON(scryfallString))
            {
                Logger.Warn(LogSource, $"Provided JSON is not a valid Scryfall JSON file.");
                validScryfallJson = false;
            }

            // If the provided JSON is not found or not set, look in the executing directory for one to use
            if (!validScryfallJson)
            {
                Logger.Trace(LogSource, $"Attempting to pull Scryfall file from executing directory.");

                string scryfallDirectory = Path.Combine(GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "scryfall");

                if (Directory.Exists(scryfallDirectory))
                {
                    string[] scryfallFiles = Directory.GetFiles(scryfallDirectory, "*.json");

                    if (scryfallFiles.Length <= 0)
                    {
                        Logger.Error(LogSource, $"No JSON files found in Scryfall directory '{scryfallDirectory}'!");
                        return false;
                    }

                    // Check each file in the directory for validity
                    foreach (string file in scryfallFiles)
                    {
                        if (IsScryfallJSON(file))
                        {
                            ScryfallPath = file;
                            Logger.Debug(LogSource, $"Scryfall JSON found in executing directory.");
                        }
                    }

                    if (string.IsNullOrEmpty(ScryfallPath))
                    {
                        Logger.Error(LogSource, $"Unable to find Scryfall JSON in executing directory!");
                    }
                }
                else
                {
                    Logger.Error(LogSource, $"Unable to find Scryfall directory '{scryfallDirectory}'!");
                    return false;
                }
            }
            else
            {
                ScryfallPath = scryfallString;
                Logger.Debug(LogSource, $"Scryfall JSON pulled from config: '{ScryfallPath}'.");
            }

            return true;
        }

        /// <summary>
        /// Try to find paths proximity directory
        /// </summary>
        private static bool ParseProximityPath(JsonElement configOptions)
        {
            // Find path to proximity files
            string proximityString = configOptions.GetProperty("ProximityDirectory").GetString();
            bool validProximityString = true;

            if (string.IsNullOrEmpty(proximityString))
            {
                Logger.Debug(LogSource, $"Path to Proximity directory not supplied.");
                validProximityString = false;
            }
            else if (!Directory.Exists(proximityString))
            {
                Logger.Warn(LogSource, $"Proximity directory not found at '{ProximityDirectory}'.");
                validProximityString = false;
            }
            else if (Directory.Exists(proximityString) && !IsProximityDirectory(proximityString))
            {
                Logger.Warn(LogSource, $"Provided directory is not a valid Proximity directory.");
                validProximityString = false;
            }
            
            // If the provided proximity directory doesn't work or is not supplied, check the executing directory
            if (!validProximityString)
            {
                Logger.Trace(LogSource, $"Attempting to find Proximity files in executing directory.");

                string executingDirectory = GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                if (!IsProximityDirectory(executingDirectory))
                {
                    Logger.Error(LogSource, $"Unable to find Proximity files in executing directory!");
                    return false;
                }
                else
                {
                    ProximityDirectory = executingDirectory;
                    Logger.Debug(LogSource, $"Proximity files found in executing directory.");
                }
            }
            else
            {
                ProximityDirectory = proximityString;
                Logger.Debug(LogSource, $"Proximity files found from config: '{ProximityDirectory}'.");
            }

            return true;
        }

        /// <summary>
        /// Determine whether a given file is a potentially valid scryfall json file
        /// </summary>
        private static bool IsScryfallJSON(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Logger.Trace(LogSource, $"'{filePath} does not exist.");
                return false;
            }

            if (!filePath.EndsWith(".json"))
            {
                Logger.Trace(LogSource, $"'{filePath} is not a JSON file.");
                return false;
            }

            try
            {
                using (FileStream fs = File.OpenRead(filePath))
                using (JsonDocument document = JsonDocument.Parse(fs))
                {
                    JsonElement root = document.RootElement;

                    if (root.GetArrayLength() <= 0)
                    {
                        Logger.Trace(LogSource, $"'{filePath}' is empty.");
                        return false;
                    }

                    if (root[0].GetProperty("object").GetString() != "card")
                    {
                        Logger.Trace(LogSource, $"'{filePath}' does not contain any card objects.");
                        return false;
                    }
                }
            }
            catch (FileNotFoundException e)
            {
                Logger.Trace(LogSource, $"JSON file '{filePath}' not found: {e.Message}");

                return false;
            }
            catch (JsonException e)
            {
                Logger.Trace(LogSource, $"JSON error reading '{filePath}': {e.Message}");

                return false;
            }
            catch (Exception e)
            {
                Logger.Trace(LogSource, $"{e.Message}");

                return false;
            }

            Logger.Trace(LogSource, $"'{filePath}' is a valid Scryfall JSON file.");
            return true;
        }

        private static bool IsProximityDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Logger.Trace(LogSource, $"Proximity directory '{directory}' does not exist.");
                return false;
            }

            string proximityFile = Path.Combine(directory, "proximity-0.6.2.jar");
            if (!File.Exists(proximityFile))
            {
                Logger.Trace(LogSource, $"Proximity directory '{directory}' does not contain a Proximity file.");
                return false;
            }

            string templateFile = Path.Combine(directory, "templates", "hlf.zip");
            if (!File.Exists(templateFile))
            {
                Logger.Trace(LogSource, $"Proximity directory '{directory}' does not contain a Proximity template file.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determine what art extension to use. Defaults to '.jpg'
        /// </summary>
        private static void ParseArtExtension(JsonElement configOptions)
        {
            ArtFileExtension = configOptions.GetProperty("ArtFileExtension").GetString().ToLower();
            if (ArtFileExtension != ".jpg" && ArtFileExtension != ".jpeg" && ArtFileExtension != ".png")
            {
                Logger.Warn(LogSource, $"Invalid art file extension '{ArtFileExtension}'. Defaulting to '{defaultArtFileExtension}'.");
                ArtFileExtension = defaultArtFileExtension;
            }
            else
            {
                Logger.Debug(LogSource, $"Art file extension pulled from config: '{ArtFileExtension}'.");
            }
        }

        /// <summary>
        /// Determine if we're overriding card rarity so the whole set matches
        /// </summary>
        private static void ParseRarityOverride(JsonElement configOptions)
        {
            ProxyRarityOverride = configOptions.GetProperty("ProxyRarityOverride").GetString().ToLower();
            if (string.IsNullOrEmpty(ProxyRarityOverride))
            {
                Logger.Debug(LogSource, $"No proxy rarity override supplied. Using card defaults from Scryfall.");
            }
            else if (!validRarities.Contains(ProxyRarityOverride))
            {
                Logger.Warn(LogSource, $"Invalid proxy rarity override supplied: {ProxyRarityOverride}. Using card defaults from Scryfall.");
                ProxyRarityOverride = "";
            }
            else
            {
                Logger.Debug(LogSource, $"Proxy rarity override pulled from config: '{ProxyRarityOverride}'.");
            }
        }

        /// <summary>
        /// Determine whether we're cleaning up the generated proxies, or leaving them raw
        /// </summary>
        private static void ParseProxyCleanup(JsonElement configOptions)
        {
            DeleteBadFaces = configOptions.GetProperty("DeleteBadFaces").GetBoolean();
            if (DeleteBadFaces)
                Logger.Debug(LogSource, $"Bad proxy faces will be deleted once all proxies have been rendered.");
            else
                Logger.Warn(LogSource, $"Bad proxy faces will not be automatically deleted.");
        }

        /// <summary>
        /// Determine if any sets are illegal in the format
        /// </summary>
        private static void ParseIllegalSetCodes(JsonElement configOptions)
        {
            IllegalSetCodes.Clear();
            JsonElement illegalSetCodeElement = configOptions.GetProperty("IllegalSetCodes");
            for (int i = 0; i < illegalSetCodeElement.GetArrayLength(); i++)
            {
                IllegalSetCodes.Add(illegalSetCodeElement[i].ToString().ToLower());
                Logger.Trace(LogSource, $"Added {IllegalSetCodes[i]} to list of illegal sets.");
            }

            Logger.Debug(LogSource, $"Loaded {IllegalSetCodes.Count} illegal sets.");
        }

        /// <summary>
        /// Determine whether we're using the full card list, or only the subset in the config file
        /// </summary>
        private static void ParseCardSubset(JsonElement configOptions)
        {
            UseCardSubset = configOptions.GetProperty("UseCardSubset").GetBoolean();
            if (UseCardSubset)
            {
                Logger.Warn(LogSource, $"Not using the full format, only the subset specified in the config file!");

                CardSubset.Clear();
                JsonElement cardSubsetElement = configOptions.GetProperty("CardSubset");
                for (int i = 0; i < cardSubsetElement.GetArrayLength(); i++)
                {
                    string specifiedCard = cardSubsetElement[i].ToString().ToLower();

                    if (!specifiedCard.Contains("//"))
                    {
                        Logger.Warn(LogSource, $"Specified card '{specifiedCard}' does not have '//'. Please double check that you have the full card name.");
                    }
                    else
                    {
                        CardSubset.Add(specifiedCard);
                        Logger.Trace(LogSource, $"Added '{CardSubset.Last()}' to list of specified cards.");
                    }
                }

                Logger.Debug(LogSource, $"Loaded {CardSubset.Count} specified cards.");
            }
            else
            {
                Logger.Debug(LogSource, $"Using all legal cards.");
            }
        }

        /// <summary>
        /// Find any cards for which we need to manually override the artist
        /// These will typically be one of two things: An artist duo with an ampersand, or the back side of an adventure
        /// </summary>
        private static void ParseManualArtistOverrides(JsonElement configOptions)
        {
            ManualArtistOverrides.Clear();
            JsonElement artistOverridesElement = configOptions.GetProperty("ManualArtistOverrides");
            for (int i = 0; i < artistOverridesElement.GetArrayLength(); i++)
            {
                JsonElement cardElement = artistOverridesElement[i];

                // Pull the card name that needs its artist overridden
                string card = cardElement.GetProperty("card").ToString().ToLower();
                if (string.IsNullOrEmpty(card))
                {
                    Logger.Error(LogSource, $"Skipping artist override with no card name!");
                    continue;
                }
                else if (!card.Contains("//"))
                {
                    Logger.Warn(LogSource, $"Artist override '{card}' does not have '//'. Please double check that you have the full card name.");
                    continue;
                }

                // Determine the artist name to use
                string artist = cardElement.GetProperty("artist").ToString();
                if (string.IsNullOrEmpty(artist))
                {
                    Logger.Error(LogSource, $"Skipping artist override for '{card}' with no artist name!");
                    continue;
                }

                // Determine whether the card is a front or back face. Default to back
                CardFace face = CardFace.Back;
                string faceString = cardElement.GetProperty("face").ToString().ToLower();
                if (faceString == "front")
                    face = CardFace.Front;
                else if (faceString != "back")
                    Logger.Warn(LogSource, $"Manual artist override for '{card}' has its face improperly specified. Defaulting to 'Back'.");

                ManualArtistOverrides.Add(new ManualArtistOverride(card, face, artist));
                Logger.Trace(LogSource, $"Added '{ManualArtistOverrides.Last().CardName}' to list of artist overrides.");
            }

            Logger.Debug(LogSource, $"Loaded {ManualArtistOverrides.Count} manual artist overrides.");
        }

        private static string GetDirectoryName(string fullPath)
        {
            return fullPath.Replace(fullPath.Split(Path.DirectorySeparatorChar).Last(), "");
        }
    }

    public class ManualArtistOverride
    {
        public string CardName { get; private set; }
        public CardFace CardFace { get; private set; }
        public string Artist { get; private set; }

        public ManualArtistOverride(string name, CardFace face, string artist)
        {
            CardName = name;
            CardFace = face;
            Artist = artist;
        }
    }
}
