using System.Text;

namespace HalfMagicProximity
{
    public class ProximityManager
    {
        private const string LogSource = "ProximityManager";
        private const string ProximityJarName = "proximity-0.6.2.jar";

        private List<CardData> allCards;
        private List<ProximityBatch> batches = new List<ProximityBatch>();

        public ProximityManager(List<CardData> allCards)
        {
            this.allCards = allCards ?? throw new ArgumentNullException(nameof(allCards));
        }

        public void Run()
        {
            int batchEstimate = allCards.Count / ProximityBatch.MaxCardCount + 1;
            Logger.Info(LogSource, $"Splitting {allCards.Count} cards into an estimated {batchEstimate} batches.");

            int processedCardCount = 0;

            while (processedCardCount < allCards.Count)
            {
                string batchName = "batch" + batches.Count;
                batches.Add(new ProximityBatch(batchName, ProximityJarName));

                // Add cards to the most recently created batch until it's full, then create a new one
                do
                {
                    batches.Last().AddCard(allCards[processedCardCount]);
                    processedCardCount++;

                    Logger.Debug(LogSource, $"{allCards[processedCardCount].DisplayName} added to {batchName} ({processedCardCount}/{allCards.Count}).");
                }
                while (!batches.Last().IsFull && processedCardCount < allCards.Count);
            }

            Logger.Info(LogSource, $"{batches.Count} batches successfully created.");

            for (int i = 0; i < batches.Count; i++)
            {
                Logger.Info(LogSource, $"Running batch {i + 1} of {batches.Count}.");
                batches[i].Run();
                Logger.Info(LogSource, $"Completed batch {i + 1} of {batches.Count}.");
            }
        }

    }
}
