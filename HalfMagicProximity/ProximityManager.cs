namespace HalfMagicProximity
{
    /// <summary>
    /// ProximityManager handles all functionality related to running the Proximity program
    /// </summary>
    public class ProximityManager
    {
        private const string LogSource = "ProximityManager";
        private const string ProximityFileName = "proximity-0.6.2.jar";
        private const string BatchNameBase = "hlf_";
        private const string RerenderBatchNameBase = BatchNameBase + "rerender_";

        private List<CardData> allCards;
        private List<ProximityBatch> batches = new List<ProximityBatch>();
        private List<ProximityBatch> rerenderBatches = new List<ProximityBatch>();

        public ProximityManager(List<CardData> allCards)
        {
            this.allCards = allCards ?? throw new ArgumentNullException(nameof(allCards));
        }

        public void Run()
        {
            int batchEstimate = allCards.Count / ProximityBatch.MaxCardCount + 1;
            Logger.Info(LogSource, $"Splitting {allCards.Count} cards into an estimated {batchEstimate} batches.");

            // Sort all cards into batches
            int processedCardCount = 0;
            while (processedCardCount < allCards.Count)
            {
                string batchName = BatchNameBase + batches.Count;
                ProximityBatch thisBatch = new ProximityBatch(this, batchName, ProximityFileName);
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

            // Run each batch in sequence
            for (int i = 0; i < batches.Count; i++)
            {
                Logger.Info(LogSource, $"Rendering batch {i + 1} of {batches.Count}.");
                batches[i].Run();
                Logger.Info(LogSource, $"Completed batch {i + 1} of {batches.Count}.");
            }

            // Attempt to rerender any cards that need it
            if (failedRenderCount > 0)
            {
                Logger.Warn(LogSource, $"There are {failedRenderCount} cards that failed to render. Trying to rerender them now.");
                AttemptRerender();
            }
        }

        private int failedRenderCount = 0;
        private int rerenderAttempts = 0;
        // ARGTODO: Make MaxRerenderAttempts a config setting
        private const int MaxRerenderAttempts = 3;
        public void HandleFailedRender(string failedCard)
        {
            failedRenderCount++;

            Logger.Trace(LogSource, $"Received '{failedCard}' as a card to rerender.");

            // No need to track failed renders if we're out of rerender attempts
            if (rerenderAttempts >= MaxRerenderAttempts) return;

            if (string.IsNullOrEmpty(failedCard))
            {
                Logger.Error(LogSource, $"Unable to determine name for failed card render. Cannot try again!");
                return;
            }

            if (rerenderBatches.Count == rerenderAttempts)
                rerenderBatches.Add(new ProximityBatch(this, RerenderBatchNameBase + rerenderAttempts, ProximityFileName));
            
            // Retry both front and back to be safe
            CardData[] cardsToRerender = allCards.Where(x => x.Name.ToLower().Contains(failedCard.ToLower())).ToArray();

            Logger.Trace(LogSource, $"There are {cardsToRerender.Length} potential cards to rerender for '{failedCard}'.");

            foreach (CardData cardData in cardsToRerender)
                rerenderBatches[rerenderAttempts].AddCard(cardData);
        }

        private void AttemptRerender()
        {
            if (rerenderBatches.Count > rerenderAttempts)
            {
                failedRenderCount = 0;
                int currentAttempt = rerenderAttempts;

                // Need to increment rerender count before trying to rerun so new failures are added to a new batch instead of the current one
                rerenderAttempts++;

                Logger.Info(LogSource, $"Beginning rerender attempt {currentAttempt + 1}.");
                rerenderBatches[currentAttempt].Run();
                Logger.Info(LogSource, $"Completed rerender attempt {currentAttempt + 1}.");

                // If after the rerender batch completes there are failed renders in queue, try again
                if (failedRenderCount > 0)
                {
                    Logger.Warn(LogSource, $"There are still {failedRenderCount} card{(failedRenderCount == 1 ? "" : "s")} that failed to render.");

                    if (rerenderAttempts >= MaxRerenderAttempts)
                    {
                        Logger.Error(LogSource, $"No more retries available. Unable to completely render cards!");
                        Logger.Debug(LogSource, $"If you know which cards failed to render, you can add them to 'CardSubset' in the config file ...");
                        Logger.Debug(LogSource, $"... and set 'UseCardSubset' to 'true' then relaunch the app to manually rerender those cards.");
                        return;
                    }

                    // Add 1 to account for zero indexing
                    int remainingTries = MaxRerenderAttempts - (rerenderAttempts + 1);
                    if (remainingTries == 0)
                        Logger.Debug(LogSource, $"This is the last rerender attempt.");
                    else if (remainingTries == 1)
                        Logger.Debug(LogSource, $"There's still {remainingTries} try remaining.");
                    else
                        Logger.Debug(LogSource, $"There are still {remainingTries} tries remaining.");

                    AttemptRerender();
                }
                else
                {
                    Logger.Info(LogSource, $"Rerender completed successfully!");
                }
            }
        }
    }
}
