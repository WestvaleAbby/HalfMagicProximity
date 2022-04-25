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

        private string batchName => "batch" + batches.Count;
        public void Run()
        {
            int batchEstimate = allCards.Count / ProximityBatch.MaxCardCount + 1;
            Logger.Info(LogSource, $"Splitting {allCards.Count} cards into an estimated {batchEstimate} batches.");

            List<CardData> batchCards = new List<CardData>();

            for (int i = 0; i < allCards.Count; i++)
            {
                // Close out the previous batch and start a new one
                if (i % ProximityBatch.MaxCardCount == 0 && batchCards.Count > 0)
                {
                    batches.Add(new ProximityBatch(batchName, ProximityJarName, batchCards));
                }

                batchCards.Add(allCards[i]);
                Logger.Debug(LogSource, $"{allCards[i].DisplayName} added to {batchName} ({batchCards.Count}/{ProximityBatch.MaxCardCount}).");
            }

            if (batchCards.Count > 0)
                batches.Add(new ProximityBatch(batchName, ProximityJarName, batchCards));

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
