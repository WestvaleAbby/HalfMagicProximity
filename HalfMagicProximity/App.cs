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
                ArtManager artManager = new ArtManager(cardManager.Cards);

                proximityManager.Run();

                if (ConfigManager.DeleteBadFaces)
                    artManager.CleanProxies();
            }
            else
            {
                Logger.Error("App", $"Invalid config file!");
            }
        }
    }
}