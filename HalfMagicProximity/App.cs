namespace HalfMagicProximity // Note: actual namespace depends on the project name.
{
    internal class App
    {
        static void Main(string[] args)
        {
            ConfigManager.Init();

            if (ConfigManager.Valid)
            {
                CardManager cardManager = new CardManager();

                cardManager.ParseJson(ConfigManager.ScryfallPath);
            }
            else
            {
                Logger.Error($"Invalid config file!");
            }
        }
    }
}