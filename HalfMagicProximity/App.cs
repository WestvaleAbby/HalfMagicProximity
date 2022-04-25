namespace HalfMagicProximity
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

                ProximityManager proximityManager = new ProximityManager(cardManager.Cards);

                proximityManager.Run();
            }
            else
            {
                Logger.Error("App", $"Invalid config file!");
            }
        }
    }
}