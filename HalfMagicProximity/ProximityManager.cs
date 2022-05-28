namespace HalfMagicProximity
{
    /// <summary>
    /// ProximityManager handles all functionality related to running the Proximity program
    /// </summary>
    public class ProximityManager
    {
        private const string LogSource = "ProximityManager";
        private const string ProximityFileName = "proximity-0.6.2.jar";
        private string BatchNameBase => "hlf_" + (renderingSketches ? "sketch_" : "");
        private string RerenderBatchNameBase => BatchNameBase + "rerender_";

        private List<CardData> allCards;
        private List<ProximityBatch> batches = new List<ProximityBatch>();
        private List<ProximityBatch> rerenderBatches = new List<ProximityBatch>();

        private bool renderingSketches = false;

        public ProximityManager(List<CardData> allCards)
        {
            this.allCards = allCards ?? throw new ArgumentNullException(nameof(allCards));
        }

        public void Run(bool isSketch)
        {
            // Flush out any previous runs
            batches.Clear();
            rerenderBatches.Clear();
            failedRenderCount = 0;
            rerenderAttempts = 0;
            renderingSketches = isSketch;

            // Determine which cards are being rendered
            List<CardData> cardsToRender = allCards;
            if (renderingSketches)
                cardsToRender = allCards.Where(x => x.UseSketchTemplate).ToList();

            int batchEstimate = cardsToRender.Count / ConfigManager.BatchSize + 1;
            Logger.Info(LogSource, $"Splitting {cardsToRender.Count} cards into an estimated {batchEstimate} batches.");

            // Sort cards into batches
            CreateBatches(cardsToRender);

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

        private void CreateBatches(List<CardData> cards)
        {
            int processedCardCount = 0;
            while (processedCardCount < cards.Count)
            {
                string batchName = BatchNameBase + batches.Count;
                ProximityBatch thisBatch = new ProximityBatch(this, batchName, ProximityFileName, renderingSketches);
                batches.Add(thisBatch);

                // Add cards to the most recently created batch until it's full, then create a new one
                do
                {
                    thisBatch.AddCard(cards[processedCardCount]);
                    processedCardCount++;

                    Logger.Trace(LogSource, $"Processed {processedCardCount} out of {cards.Count}.");
                }
                while (!thisBatch.IsFull && processedCardCount < cards.Count);
            }
            Logger.Info(LogSource, $"{batches.Count} batches successfully created.");
        }

        private int failedRenderCount = 0;
        private int rerenderAttempts = 0;
        public void HandleFailedRender(string failedCard)
        {
            failedRenderCount++;

            Logger.Trace(LogSource, $"Received '{failedCard}' as a card to rerender.");

            // No need to track failed renders if we're out of rerender attempts
            if (rerenderAttempts >= ConfigManager.MaxRetries) return;

            if (string.IsNullOrEmpty(failedCard))
            {
                Logger.Error(LogSource, $"Unable to determine name for failed card render. Cannot try again!");
                return;
            }

            if (rerenderBatches.Count == rerenderAttempts)
                rerenderBatches.Add(new ProximityBatch(this, RerenderBatchNameBase + rerenderAttempts, ProximityFileName, renderingSketches));

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

                    if (rerenderAttempts >= ConfigManager.MaxRetries)
                    {
                        Logger.Error(LogSource, $"No more retries available. Unable to completely render cards!");
                        Logger.Debug(LogSource, $"If you know which cards failed to render, you can add them to 'CardSubset' in the config file ...");
                        Logger.Debug(LogSource, $"... and set 'UseCardSubset' to 'true' then relaunch the app to manually rerender those cards.");
                        return;
                    }

                    // Add 1 to account for zero indexing
                    int remainingTries = ConfigManager.MaxRetries - (rerenderAttempts + 1);
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
