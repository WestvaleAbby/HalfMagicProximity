namespace HalfMagicProximity // Note: actual namespace depends on the project name.
{
    internal class App
    {
        // ARGTODO: Pull from config
        const string SCRYFALL_PATH = "D:\\Personal Files\\Docs\\Magic\\HLF Proximity\\oracle-cards-20220423210218.json";

        static void Main(string[] args)
        {
            // ARGTODO: Pull from config
            Logger.IsDebugEnabled = true;

            CardManager cardManager = new CardManager();

            cardManager.ParseJson(SCRYFALL_PATH);
        }
    }
}