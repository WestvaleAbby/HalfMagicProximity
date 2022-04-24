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

            // Generate deck files
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

            // Add color override if faces have different colors
            if (card.NeedsColorOverride)
            {
                cardString += OverrideTemplate + "colors:[\"" + card.Color + "\"]";
                cardString += OverrideTemplate + "proximity.mtg.color_count:" + card.ColorCount;
            }

            if (card.NeedsArtistOverride)
                cardString += OverrideTemplate + "artist:\"" + card.Artist + "\"";

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
