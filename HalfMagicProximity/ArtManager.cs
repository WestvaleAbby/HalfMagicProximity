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

        public void CleanProxies(CardTemplate template)
        {
            string executingDirectory = GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputDirectory = ConfigManager.OutputDirectory;

            if (Directory.Exists(executingDirectory))
            {
                // Handle preparation of output directory
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);

                    Logger.Trace(LogSource, $"Created output directory: {outputDirectory}");

                    // M15 template is generated first, so if the output directory is being created on a different template, something likely went wrong earlier in the process
                    if (template == CardTemplate.Sketch)
                        Logger.Warn(LogSource, $"All non sketch proxies possibly missing from output directory: {outputDirectory}");
                    else if (template == CardTemplate.DoubleFeature)
                        Logger.Warn(LogSource, $"All non double feature proxies possibly missing from output directory: {outputDirectory}");
                }
                else if (!ConfigManager.UpdatesOnly && template == CardTemplate.M15)
                {
                    // Normal frame cards get generated first, so only clear the folder the first time this is called
                    Logger.Warn(LogSource, $"Clearing output directory of all '.png' files: {outputDirectory}");

                    foreach (string filePath in Directory.EnumerateFiles(outputDirectory))
                    {
                        if (filePath.EndsWith(".png"))
                            File.Delete(filePath);
                    }
                }

                // Get all raw proxy images
                IEnumerable<string> frontImages = Directory.EnumerateFiles(Path.Combine(executingDirectory, "images", "fronts"));
                IEnumerable<string> backImages = Directory.EnumerateFiles(Path.Combine(executingDirectory, "images", "backs"));

                int totalFileCount = frontImages.Count() + backImages.Count();
                int processedCount = 0;
                int goodProxyCount = 0;

                // Iterate through all of the proxies we've found
                foreach (string proxyFilePath in frontImages.Concat(backImages))
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

                        // Separate  the card name and the proxy number from the file name
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
                                bool deleteProxy = false;
                                if (template == CardTemplate.M15)
                                {
                                    if (cardData.Template == CardTemplate.Sketch)
                                    {
                                        // Ignore all adventure backs if we're not pulling sketches
                                        deleteProxy = true;
                                        Logger.Trace(LogSource, $"Skipping normal proxy for adventure backs. Wait for a sketch proxy.");
                                    }
                                    else if (cardData.Template == CardTemplate.Sketch)
                                    {
                                        // Ignore all adventure backs if we're not pulling sketches
                                        deleteProxy = true;
                                        Logger.Trace(LogSource, $"Skipping normal proxy for aftermath backs. Wait for a double feature proxy.");
                                    }
                                    else
                                    {
                                        // We want to delete even proxies for front faces and odd proxies for back cards
                                        deleteProxy = (cardData.Face == CardFace.Front && isEven) ||
                                            (cardData.Face == CardFace.Back && !isEven);

                                        if (deleteProxy)
                                            Logger.Trace(LogSource, $"Deleting proxy because this is a {cardData.Face} face and the proxy is {(isEven ? "even" : "odd")}.");
                                    }
                                }
                                else
                                {
                                    // We want to copy over all sketch and double feature backs regardless of number, and delete all fronts
                                    deleteProxy = cardData.Face == CardFace.Front;

                                    if (deleteProxy)
                                        Logger.Trace(LogSource, $"Deleting proxy because it's a sketch or double feature front. These frames are only used for card backs.");
                                }

                                // Copy good proxies to the output directory and rename them without the proximity render number in front
                                if (!deleteProxy)
                                {
                                    Logger.Trace(LogSource, $"Found good proxy for {cardData.DisplayName}.");
                                    string goodProxyPath = Path.Combine(outputDirectory, cardName + ExpectedExtension);

                                    // If we haven't made a proxy of this card yet then make one, otherwise ignore it
                                    // Always copy and overwrite good sketches and double features
                                    if ((!File.Exists(goodProxyPath) && template == CardTemplate.M15) || template != CardTemplate.M15)
                                    {
                                        File.Copy(proxyFilePath, goodProxyPath, true);

                                        if (File.Exists(goodProxyPath))
                                        {
                                            Logger.Debug(LogSource, $"Good proxy for {cardName} copied to output directory.");
                                            goodProxyCount++;
                                        }
                                        else
                                        {
                                            Logger.Error(LogSource, $"Unable to collect good proxy for {cardName}!");
                                        }
                                        Logger.Trace(LogSource, $"Raw proxy file no longer needed. Deleting '{fileName}'.");
                                    }
                                    else
                                    {
                                        Logger.Trace(LogSource, $"Found a duplicate good proxy. Deleting.");
                                    }
                                }
                                else
                                {
                                    Logger.Trace(LogSource, $"Found bad proxy for {cardName}. Deleting '{fileName}'.");
                                }
                            }
                            else
                            {
                                Logger.Warn(LogSource, $"Unable to find card data for '{fileName}'.");
                            }

                            processedCount++;
                            Logger.Trace(LogSource, $"Processed {processedCount} of {totalFileCount} potential proxies.");
                        }

                        try
                        {
                            // Delete file once we're done with it to keep things clean for next run
                            File.Delete(proxyFilePath);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(LogSource, ex.Message);
                        }
                    }
                    else
                    {
                        Logger.Warn(LogSource, $"Unable to find file '{proxyFilePath}'.");
                    }
                }

                // Output all failed cards so the user knowns which to retry
                int failedProxies = 0;
                foreach (CardData thisCard in cards)
                {
                    // Don't want to report cards that are generated with a different template
                    if (!File.Exists(Path.Combine(outputDirectory, thisCard.DisplayName + ExpectedExtension)) && template == thisCard.Template)
                    {
                        Logger.Warn(LogSource, $"No proxy found for {thisCard.DisplayName}.");
                        failedProxies++;
                    }
                }
                if (failedProxies > 0)
                {
                    Logger.Error(LogSource, $"{failedProxies} cards did not have successful proxies generated!");
                    Logger.Debug(LogSource, $"Consider specifying failed cards in the config file and running again.");
                }

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
