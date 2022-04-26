using System.Reflection;
using System.Text.Json;

namespace HalfMagicProximity
{
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

        public static bool UseDebugCardSubset;
        public static List<string> DebugCards = new List<string>();

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
                    if (!ParsePaths(configOptions)) return;
                    ParseArtExtension(configOptions);
                    ParseRarityOverride(configOptions);
                    ParseProxyCleanup(configOptions);
                    ParseIllegalSetCodes(configOptions);
                    ParseDebugCardSet(configOptions);
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
        /// Try to find paths for scryfall JSON and proximity directory
        /// </summary>
        private static bool ParsePaths(JsonElement configOptions)
        {
            // Find path to scryfall json
            string scryfallString = configOptions.GetProperty("ScryfallPath").GetString();
            if (string.IsNullOrEmpty(scryfallString))
            {
                Logger.Error(LogSource, $"Path to Scryfall JSON not supplied!");
                return false;
            }
            else if (!File.Exists(scryfallString))
            {
                Logger.Error(LogSource, $"Scryfall JSON not found at '{scryfallString}'!");
                return false;
            }
            else
            {
                ScryfallPath = scryfallString;
                Logger.Debug(LogSource, $"Scryfall path pulled from config: '{ScryfallPath}'.");
            }

            // Find path to proximity files
            string proximityString = configOptions.GetProperty("ProximityDirectory").GetString();
            if (string.IsNullOrEmpty(proximityString))
            {
                Logger.Error(LogSource, $"Path to Proximity directory not found!");
                return false;
            }
            else if (!Directory.Exists(proximityString))
            {
                Logger.Error(LogSource, $"Proximity directory not found at '{ProximityDirectory}'!");
                return false;
            }
            else
            {
                ProximityDirectory = proximityString;
                Logger.Debug(LogSource, $"Proximity directory pulled from config: '{ProximityDirectory}'.");
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
        /// Determine whether we're using the full card list, or only the debug cards
        /// </summary>
        private static void ParseDebugCardSet(JsonElement configOptions)
        {
            UseDebugCardSubset = configOptions.GetProperty("UseDebugCardSubset").GetBoolean();
            if (UseDebugCardSubset)
            {
                Logger.Warn(LogSource, $"Not using the full format, only the debug subset!");

                DebugCards.Clear();
                JsonElement debugCardElement = configOptions.GetProperty("DebugCards");
                for (int i = 0; i < debugCardElement.GetArrayLength(); i++)
                {
                    string debugCard = debugCardElement[i].ToString().ToLower();

                    if (!debugCard.Contains("//"))
                    {
                        Logger.Warn(LogSource, $"Debug card '{debugCard}' does not have '//'. Please double check that you have the full card name.");
                    }
                    else
                    {
                        DebugCards.Add(debugCard);
                        Logger.Trace(LogSource, $"Added '{DebugCards.Last()}' to list of debug cards.");
                    }
                }

                Logger.Debug(LogSource, $"Loaded {DebugCards.Count} debug cards.");
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
