using System.Text;

namespace HalfMagicProximity
{
    public class ProximityManager
    {
        private const string LogSource = "ProximityManager";
        private const string ProximityFileName = "proximity-0.6.2.jar";
        private const string BatchNameBase = "hlf_";

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
                string batchName = BatchNameBase + batches.Count;
                ProximityBatch thisBatch = new ProximityBatch(batchName, ProximityFileName);
                batches.Add(thisBatch);

                // Add cards to the most recently created batch until it's full, then create a new one
                do
                {
                    thisBatch.AddCard(allCards[processedCardCount]);
                    processedCardCount++;

                    Logger.Trace(LogSource, $"Processed {processedCardCount} out of {allCards.Count}.");
                }
                while (!thisBatch.IsFull && processedCardCount < allCards.Count);
            }

            Logger.Info(LogSource, $"{batches.Count} batches successfully created.");

            for (int i = 0; i < batches.Count; i++)
            {
                Logger.Info(LogSource, $"Rendering batch {i + 1} of {batches.Count}.");
                batches[i].Run();
                Logger.Info(LogSource, $"Completed batch {i + 1} of {batches.Count}.");
            }
        }

    }
}
