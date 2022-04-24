using System.Reflection;
using System.Text.Json;

namespace HalfMagicProximity
{
    static class ConfigManager
    {
        static string ScryfallPath;
        static string ProximityDirectory;

        static public bool Valid => !string.IsNullOrEmpty(ScryfallPath) && !string.IsNullOrEmpty(ProximityDirectory);

        private const string CONFIG_FILE_NAME = "hlfconfig.json";

        static public void Init()
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

                    ScryfallPath = configOptions.GetProperty("ScryfallPath").GetString();
                    if (string.IsNullOrEmpty(ScryfallPath)) throw new Exception($"Path to Scryfall JSON not found!");

                    ProximityDirectory = configOptions.GetProperty("ProximityDirectory").GetString();
                    if (string.IsNullOrEmpty(ProximityDirectory)) throw new Exception($"Path to Proximity Directory not found!");
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
