using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            // Add cards to file
            foreach (var card in allCards)
            {
                GenerateCardString(card);
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
                string artPath = Path.Combine(ConfigManager.ProximityDirectory, "art", "back", card.ArtFileName).Replace("\\", "/");

                cardString += OverrideTemplate + "image_uris.art_crop:\"\"file:///" + artPath + "\"\"";
            }

            Logger.Debug(cardString);

            return cardString;
        }
    }
}
