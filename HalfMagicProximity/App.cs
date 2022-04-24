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

            Logger.Log(Severity.Info, "this is an info message");
            Logger.Info("this is an info message");

            Logger.Log(Severity.Warn, "this is a warning message");
            Logger.Warn("this is a warning message");

            Logger.Log(Severity.Error, "this is an error message");
            Logger.Error("this is an error message");

            Logger.Log(Severity.Debug, "this is a debug message");
            Logger.Debug("this is a debug message");

            Logger.IsDebugEnabled = true;
            Logger.Log(Severity.Debug, "this is a debug message");
            Logger.Debug("this is a debug message");
        }
    }
}