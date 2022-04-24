namespace HalfMagicProximity // Note: actual namespace depends on the project name.
{
    internal class App
    {
        // ARGTODO: Pull these from config?
        const string SCRYFALL_PATH = "D:\\Personal Files\\Docs\\Magic\\HLF Proximity\\oracle-cards-20220423210218.json";

        static void Main(string[] args)
        {
            CardManager cardManager = new CardManager();

            cardManager.ParseJson(SCRYFALL_PATH);
        }
    }
}