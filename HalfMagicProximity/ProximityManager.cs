using System.Text;

namespace HalfMagicProximity
{
    public class ProximityManager
    {
        private List<CardData> allCards;

        public ProximityManager(List<CardData> allCards)
        {
            this.allCards = allCards ?? throw new ArgumentNullException(nameof(allCards));
        }

        public void Run()
        {
            Logger.Info("Beginning Proximity preparations.");

            GenerateDeckFiles();

            // Generate command string

            // Verify that everything we need for proximity is present
            
            // Run proximity
        }

        private void GenerateDeckFiles()
        {
            // Create or open and clear file
            string deckPath = Path.Combine(ConfigManager.ProximityDirectory, "cards.txt");

            try
            {
                int successfulCards = 0;

                using (FileStream deckStream = File.Create(deckPath))
                {
                    // Add cards to file
                    foreach (CardData card in allCards)
                    {
                        string cardString = GenerateCardString(card);
                        byte[] cardBytes = new UTF8Encoding(true).GetBytes(cardString + Environment.NewLine);

                        // Write card string to the deck file
                        deckStream.Write(cardBytes, 0, cardBytes.Length);
                        successfulCards++;
                    }
                }

                if (File.Exists(deckPath))
                    Logger.Info($"Generated deck file with {successfulCards} at '{deckPath}'.");
                else
                    Logger.Error($"Unable to generate deck file at '{deckPath}'!");
            }
            catch (DirectoryNotFoundException e)
            {
                Logger.Error($"Unable to find Proximity directory '{ConfigManager.ProximityDirectory}'!");
            }
            catch (Exception e)
            {
                Logger.Error($"Error generating proximity deck file: {e.Message}");
            }
        }

        private const string OverrideTemplate = " --override=";

        private string GenerateCardString(CardData card)
        {
            string cardString = $"1 {card.Name}";

            // Add rarity override if one was specified in the config settings
            if (ConfigManager.IsProxyRarityOverrided)
                cardString += OverrideTemplate + "rarity:" + ConfigManager.ProxyRarityOverride;

            // Add color overrides if faces have different colors
            if (card.NeedsColorOverride)
            {
                cardString += OverrideTemplate + "colors:[\"" + card.Color + "\"]";
                cardString += OverrideTemplate + "proximity.mtg.color_count:" + card.ColorCount;
            }

            // Add artist override if faces have different artists
            if (card.NeedsArtistOverride)
                cardString += OverrideTemplate + "artist:\"" + card.Artist + "\"";

            // Front faces can automatically pull their art directly from the art folder or scryfall. Back faces need to be manually pointed to specific art crops
            if (card.Face == CardFace.Back)
            {
                string artPath = Path.Combine(ConfigManager.ProximityDirectory, "art", "back", card.ArtFileName).Replace("\\", "/").Replace(" ", "%20");

                cardString += OverrideTemplate + "image_uris.art_crop:\"\"file:///" + artPath + "\"\"";
            }

            Logger.Debug(cardString);

            return cardString;
        }
    }
}
