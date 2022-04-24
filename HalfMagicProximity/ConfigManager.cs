using System.Reflection;
using System.Text.Json;

namespace HalfMagicProximity
{
    static class ConfigManager
    {
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

                    // Determine whether debug logs are displayed
                    Logger.IsDebugEnabled = configOptions.GetProperty("IsDebugEnabled").GetBoolean();
                    if (Logger.IsDebugEnabled)
                        Logger.Info($"Debug messages are enabled.");
                    else
                        Logger.Info($"Debug messages are disabled.");

                    // Find path to scryfall json
                    ScryfallPath = configOptions.GetProperty("ScryfallPath").GetString();
                    if (string.IsNullOrEmpty(ScryfallPath))
                        throw new Exception($"Path to Scryfall JSON not supplied!");
                    else if (!File.Exists(ScryfallPath))
                        throw new Exception($"Scryfall JSON not found at '{ScryfallPath}'!");
                    else
                        Logger.Info($"Scryfall path pulled from config: '{ScryfallPath}'.");

                    // Find path to proximity files
                    ProximityDirectory = configOptions.GetProperty("ProximityDirectory").GetString();
                    if (string.IsNullOrEmpty(ProximityDirectory)) 
                        throw new Exception($"Path to Proximity directory not found!");
                    else if (!Directory.Exists(ProximityDirectory))
                        throw new Exception($"Proximity directory not found at '{ProximityDirectory}'!");
                    else
                        Logger.Info($"Proximity directory pulled from config: '{ProximityDirectory}'.");

                    // Determine what art extension to use. Defaults to '.jpg'
                    ArtFileExtension = configOptions.GetProperty("ArtFileExtension").GetString().ToLower();
                    if (ArtFileExtension != ".jpg" && ArtFileExtension != ".jpeg" && ArtFileExtension != ".png")
                    {
                        Logger.Warn($"Invalid art file extension '{ArtFileExtension}'. Defaulting to '{defaultArtFileExtension}'.");
                        ArtFileExtension = defaultArtFileExtension;
                    }
                    else
                    {
                        Logger.Info($"Art file extension pulled from config: '{ArtFileExtension}'.");
                    }

                    // Determine if we're overriding card rarity so the whole set matches
                    ProxyRarityOverride = configOptions.GetProperty("ProxyRarityOverride").GetString().ToLower();
                    if (string.IsNullOrEmpty(ProxyRarityOverride))
                    {
                        Logger.Info($"No proxy rarity override supplied. Using card defaults from Scryfall.");
                    }
                    else if (!validRarities.Contains(ProxyRarityOverride))
                    {
                        Logger.Warn($"Invalid proxy rarity override supplied: {ProxyRarityOverride}. Using card defaults from Scryfall.");
                        ProxyRarityOverride = "";
                    }
                    else
                    {
                        Logger.Info($"Proxy rarity override pulled from config: '{ProxyRarityOverride}'.");
                    }

                    // Determine whether we're cleaning up the generated proxies, or leaving them raw
                    DeleteBadFaces = configOptions.GetProperty("DeleteBadFaces").GetBoolean();
                    if (DeleteBadFaces)
                        Logger.Info($"Bad proxy faces will be deleted once all proxies have been rendered.");
                    else
                        Logger.Warn($"Bad proxy faces will not be automatically deleted.");

                    // Determine if any sets are illegal in the format
                    IllegalSetCodes.Clear();
                    JsonElement illegalSetCodeElement = configOptions.GetProperty("IllegalSetCodes");
                    for (int i = 0; i < illegalSetCodeElement.GetArrayLength(); i++)
                    {
                        IllegalSetCodes.Add(illegalSetCodeElement[i].ToString().ToLower());
                        Logger.Info($"Added {IllegalSetCodes[i]} to list of illegal sets.");
                    }

                    // Determine whether we're using the full card list, or only the debug cards
                    UseDebugCardSubset = configOptions.GetProperty("UseDebugCardSubset").GetBoolean();
                    if (UseDebugCardSubset)
                    {
                        Logger.Warn($"Not using the full format, only the debug subset!");

                        DebugCards.Clear();
                        JsonElement debugCardElement = configOptions.GetProperty("DebugCards");
                        for (int i = 0; i < debugCardElement.GetArrayLength(); i++)
                        {
                            DebugCards.Add(debugCardElement[i].ToString().ToLower());
                            Logger.Info($"Added {DebugCards[i]} to list of debug cards.");
                        }
                    }
                    else
                    {
                        Logger.Info($"Using all legal cards.");
                    }

                    // Find any cards for which we need to manually override the artist - typically the back half of adventures
                    ManualArtistOverrides.Clear();
                    JsonElement artistOverridesElement = configOptions.GetProperty("ManualArtistOverrides");
                    for (int i = 0; i < artistOverridesElement.GetArrayLength(); i++)
                    {
                        JsonElement cardElement = artistOverridesElement[i];

                        // Pull the card name that needs its artist overridden
                        string card = cardElement.GetProperty("card").ToString().ToLower();
                        if (string.IsNullOrEmpty(card))
                        {
                            Logger.Error($"Skipping artist override with no card name!");
                            continue;
                        }
                        else if (!card.Contains("//"))
                        {
                            Logger.Warn($"Artist override '{card}' does not have '//'. Double check that you have the full card name.");
                        }

                        // Determine whether the card is a front or back face. Default to front
                        CardFace face = CardFace.Front;
                        string faceString = cardElement.GetProperty("face").ToString().ToLower();
                        if (faceString == "back")
                            face = CardFace.Back;
                        else if (faceString != "front")
                            Logger.Warn($"Manual artist override for '{card}' has its face improperly specified. Defaulting to 'front'.");

                        // Determine the artist name to use
                        string artist = cardElement.GetProperty("artist").ToString().ToLower();
                        if (string.IsNullOrEmpty(artist))
                        {
                            Logger.Error($"Skipping artist override with no artist name!");
                            continue;
                        }

                        ManualArtistOverrides.Add(new ManualArtistOverride(card, face, artist));
                        Logger.Info($"Added '{ManualArtistOverrides[i].CardName}' to list of artist overrides.");
                    }
                }
            }
            catch (FileNotFoundException e)
            {
                Logger.Error($"Config file not found: {e.Message}");
            }
            catch (JsonException e)
            {
                Logger.Error($"Config JSON Error: {e.Message}");
            }
            catch (Exception e)
            {
                Logger.Error($"Config Error: {e.Message}");
            }
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
