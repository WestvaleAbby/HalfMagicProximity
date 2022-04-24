namespace HalfMagicProximity // Note: actual namespace depends on the project name.
{
    internal class App
    {
        // ARGTODO: Pull from config
        const string SCRYFALL_PATH = "D:\\Personal Files\\Docs\\Magic\\HalfMagic\\VS App\\oracle-cards-20220423210218.json";

        static void Main(string[] args)
        {
            ConfigManager.Init();

            if (ConfigManager.Valid)
            {
                CardManager cardManager = new CardManager();

                cardManager.ParseJson(SCRYFALL_PATH);
            }
            else
            {
                Logger.Error($"Invalid config file!");
            }
        }
    }
}