using System.Diagnostics;

namespace HalfMagicProximity
{
    internal class App
    {
        static void Main(string[] args)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();

            ConfigManager.Init();

            if (ConfigManager.Valid)
            {
                CardManager cardManager = new CardManager();
                cardManager.ParseJson(ConfigManager.ScryfallPath);

                ProximityManager proximityManager = new ProximityManager(cardManager.Cards);
                ArtManager artManager = new ArtManager(cardManager.Cards);

                // Generate the front faces of adventures and all split cards with the normal M15 template
                proximityManager.Run(isSketch:false);
                artManager.CleanProxies(isSketch: false);

                // Generate the back faces of adventures with the sketch template
                proximityManager.Run(isSketch:true);
                artManager.CleanProxies(isSketch:true);

                TimeSpan elapsed = timer.Elapsed;
                string elapsedString = string.Format("{0:00}:{1:00}.{2:00}", elapsed.Minutes, elapsed.Seconds, elapsed.Milliseconds / 10);
                if (elapsed.Hours > 0)
                    elapsedString = string.Format("{0:00}", elapsed.Hours) + elapsedString;

                Logger.Debug("App", $"Completed in {elapsedString}.");
            }
            else
            {
                Logger.Error("App", $"Invalid config file!");
            }
        }
    }
}