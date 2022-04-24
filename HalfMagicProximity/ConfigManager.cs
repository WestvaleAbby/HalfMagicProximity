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

                    Logger.IsDebugEnabled = configOptions.GetProperty("IsDebugEnabled").GetBoolean();
                    if (Logger.IsDebugEnabled)
                        Logger.Info($"Debug messages are enabled.");
                    else
                        Logger.Info($"Debug messages are disabled.");

                    ScryfallPath = configOptions.GetProperty("ScryfallPath").GetString();
                    if (string.IsNullOrEmpty(ScryfallPath))
                        throw new Exception($"Path to Scryfall JSON not supplied!");
                    else if (!File.Exists(ScryfallPath))
                        throw new Exception($"Scryfall JSON not found at '{ScryfallPath}'!");
                    else
                        Logger.Info($"Scryfall path pulled from config: '{ScryfallPath}'.");

                    ProximityDirectory = configOptions.GetProperty("ProximityDirectory").GetString();
                    if (string.IsNullOrEmpty(ProximityDirectory)) 
                        throw new Exception($"Path to Proximity directory not found!");
                    else if (!Directory.Exists(ProximityDirectory))
                        throw new Exception($"Proximity directory not found at '{ProximityDirectory}'!");
                    else
                        Logger.Info($"Proximity directory pulled from config: '{ProximityDirectory}'.");

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

                    DeleteBadFaces = configOptions.GetProperty("DeleteBadFaces").GetBoolean();
                    if (DeleteBadFaces)
                        Logger.Info($"Bad proxy faces will be deleted once all proxies have been rendered.");
                    else
                        Logger.Info($"Bad proxy faces will not be automatically deleted.");

                    IllegalSetCodes.Clear();
                    JsonElement illegalSetCodeElement = configOptions.GetProperty("IllegalSetCodes");
                    for (int i = 0; i < illegalSetCodeElement.GetArrayLength(); i++)
                    {
                        IllegalSetCodes.Add(illegalSetCodeElement[i].ToString().ToLower());
                        Logger.Info($"Added {IllegalSetCodes[i]} to list of illegal sets.");
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
}
