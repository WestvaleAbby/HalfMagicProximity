﻿using System.Reflection;

namespace HalfMagicProximity
{
    /// <summary>
    /// ArtManager handles all tasks related to image files generated by this app
    /// </summary>
    internal class ArtManager
    {
        private const string LogSource = "ArtManager";
        private const string ExpectedExtension = ".png";

        private List<CardData> cards;

        public ArtManager(List<CardData> cards)
        {
            this.cards = cards ?? throw new ArgumentNullException(nameof(cards));
        }

        public void CleanProxies()
        {
            string executingDirectory = GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            
            if (Directory.Exists(executingDirectory))
            {
                string outputDirectory = Path.Combine(executingDirectory, "Proxies");
                if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);

                IEnumerable<string> frontImages = Directory.EnumerateFiles(Path.Combine(executingDirectory, "images", "fronts"));
                IEnumerable<string> backImages = Directory.EnumerateFiles(Path.Combine(executingDirectory, "images", "backs"));

                int totalFileCount = frontImages.Count() + backImages.Count();
                int processedCount = 0;
                int goodProxyCount = 0;

                // Iterate through all of the proxies we've found
                foreach(string proxyFilePath in frontImages.Concat(backImages))
                {
                    string fileName = GetFileName(proxyFilePath);
                    Logger.Trace(LogSource, $"Found '{fileName}' in {GetDirectoryName(proxyFilePath)}.");
                    
                    if (File.Exists(proxyFilePath))
                    {
                        if (!fileName.EndsWith(ExpectedExtension))
                        {
                            Logger.Warn(LogSource, $"'{fileName}' does not end with extension '{ExpectedExtension}'. Cannot process as a proxy.");
                            processedCount++;
                            continue;
                        }

                        // Separeat  the card name and the proxy number from the file name
                        string[] splitFileName = fileName.Split(" ");

                        string cardName = "";
                        for (int i = 1; i < splitFileName.Length; i++)
                            cardName += splitFileName[i] + " ";
                        cardName = cardName.Replace(".png", "").Trim();

                        string number = splitFileName.First().Replace("a", "").Replace("b", "").Trim();

                        int numberInt = 0;
                        int.TryParse(number, out numberInt);

                        if (numberInt > 0)
                        {
                            bool isEven = (numberInt % 2 == 0);

                            CardData cardData = cards.Where(x => x.DisplayName.ToLower() == cardName.ToLower()).FirstOrDefault();

                            if (cardData != null)
                            {
                                // We want to delete even proxies for front faces and odd proxies for back cards
                                bool deleteProxy = (cardData.Face == CardFace.Front && isEven) ||
                                    (cardData.Face == CardFace.Back && !isEven);

                                if (deleteProxy)
                                {
                                    Logger.Trace(LogSource, $"Found bad proxy for {cardName}. Deleting '{fileName}'.");
                                    File.Delete(proxyFilePath);
                                }
                                else
                                {
                                    Logger.Debug(LogSource, $"Found good proxy for {cardData.DisplayName}.");
                                    string goodProxyPath = Path.Combine(outputDirectory, cardName + ExpectedExtension);

                                    File.Copy(proxyFilePath, goodProxyPath, true);

                                    if (File.Exists(goodProxyPath))
                                    {
                                        Logger.Debug(LogSource, $"Good proxy for {cardName} copied to '{goodProxyPath}'.");
                                        goodProxyCount++;
                                    }
                                    else
                                    {
                                        Logger.Error(LogSource, $"Unable to collect good proxy for {cardName}!");
                                    }
                                }
                            }
                            else
                            {
                                Logger.Warn(LogSource, $"Unable to find a rendered proxy for {cardName}.");
                            }

                            processedCount++;
                            Logger.Trace(LogSource, $"Processed {processedCount} of {totalFileCount} potential proxies.");
                        }
                    }
                    else
                    {
                        Logger.Warn(LogSource, $"Unable to find file '{proxyFilePath}'.");
                    }                    
                }

                int failedProxies = 0;
                foreach (CardData thisCard in cards)
                {
                    if (!File.Exists(Path.Combine(outputDirectory, thisCard.DisplayName + ExpectedExtension)))
                    {
                        Logger.Warn(LogSource, $"No proxy found for {thisCard.DisplayName}.");
                        failedProxies++;
                    }
                }
                if (failedProxies > 0)
                    Logger.Error(LogSource, $"{failedProxies} cards did not have successful proxies generated. Consider specifying them in the config file and running again!");

                Logger.Info(LogSource, $"{goodProxyCount} completed renders are available in '{outputDirectory}'!");
            }
        }

        private string GetFileName(string fullPath)
        {
            return fullPath.Split(Path.DirectorySeparatorChar).Last();
        }

        private string GetDirectoryName(string fullPath)
        {
            return fullPath.Replace(GetFileName(fullPath), "");
        }
    }
}
