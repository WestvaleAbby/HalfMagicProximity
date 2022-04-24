namespace HalfMagicProximity // Note: actual namespace depends on the project name.
{
    internal class App
    {
        // ARGTODO: Pull from config
        const string SCRYFALL_PATH = "D:\\Personal Files\\Docs\\Magic\\HalfMagic\\VS App\\oracle-cards-20220423210218.json";
        const bool DEBUG = true;

        static void Main(string[] args)
        {
            Logger.IsDebugEnabled = DEBUG;

            CardManager cardManager = new CardManager();

            cardManager.ParseJson(SCRYFALL_PATH);
        }
    }
}