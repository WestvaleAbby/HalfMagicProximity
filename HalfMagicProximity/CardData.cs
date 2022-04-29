namespace HalfMagicProximity
{
    public enum CardFace { Front, Back };
    public enum CardLayout { Split, Adventure, None };

    /// <summary>
    /// A card pulled from the Scryfall JSON
    /// </summary>
    public class CardData
    {
        private const string LogSource = "Card";
        private string namedLogSource => $"{LogSource}: {DisplayName}";

        public string Name { get; private set; }
        public string DisplayName { get; private set; }
        public string Color { get; private set; }
        public int ColorCount { get; private set; }
        public string ArtFileName { get; private set; }
        public string Artist { get; private set; }
        private bool manualArtist = false;
        public CardFace Face { get; private set; }
        public CardLayout Layout { get; private set; }
        public CardData OtherFace { private get; set; }
        public string Watermark { get; private set; }

        public bool NeedsColorOverride => Color != OtherFace.Color;
        public bool NeedsArtOverride => Face == CardFace.Back || Layout == CardLayout.Split;
        public bool NeedsArtistOverride => Artist != OtherFace.Artist || manualArtist;
        public bool NeedsWatermarkOverride => !string.IsNullOrEmpty(Watermark) || !string.IsNullOrEmpty(OtherFace.Watermark);

        public CardData(string name, string manaCost, string art, string artist, CardFace face, CardLayout layout, string watermark)
        {
            if (string.IsNullOrEmpty(name))
                Logger.Warn(LogSource, "Card object created with no name!");
            Name = name;

            Face = face;
            if (Face == CardFace.Front)
                DisplayName = Name.Split(" // ").First().Trim();
            else
                DisplayName = Name.Split(" // ").Last().Trim();

            if (string.IsNullOrEmpty(manaCost))
                Logger.Warn(namedLogSource, "Card object created with no manaCost!");
            GetColorData(manaCost);

            if (string.IsNullOrEmpty(art))
                Logger.Warn(namedLogSource, "Card object created with no art file name!");
            ArtFileName = art;

            if (string.IsNullOrEmpty(artist))
                Logger.Warn(namedLogSource, "Card object created with no artist name!");
            Artist = artist;

            if (layout == CardLayout.None)
                Logger.Warn(namedLogSource, "Card object created with no layout!");
            Layout = layout;

            // Don't need to check if watermark is empty, empty indicates no watermark
            Watermark = watermark;
        }

        /// <summary>
        /// Parses the mana cost of a card to determine its colors and color count
        /// </summary>
        /// <param name="manaCost"> The card's mana cost as a string</param>
        private void GetColorData(string manaCost)
        {
            manaCost = manaCost.ToUpper();

            char[] colors = { 'W', 'U', 'B', 'R', 'G' };

            foreach (char color in colors)
            {
                if (manaCost.Contains(color))
                {
                    Color += color;
                }
            }

            Color = CorrectColorOrder(Color);

            ColorCount = Color.Length;
        }

        private string CorrectColorOrder(string color)
        {
            // Some color pairs need to be reordered in order for proximity to recognize them properly
            switch (color)
            {
                case "UG":
                    Logger.Trace(namedLogSource, $"Correcting color from UG to GU.");
                    return "GU";
                case "WG":
                    Logger.Trace(namedLogSource, $"Correcting color from WG to GW.");
                    return "GW";
                case "WR":
                    Logger.Trace(namedLogSource, $"Correcting color from WR to RW.");
                    return "RW";
                default:
                    return color;
            }
        }

        public void CorrectArtist(string newArtist)
        {
            Logger.Trace(namedLogSource, $"Manually correcting artist from '{Artist}' to '{newArtist}'.");
            manualArtist = true;
            Artist = newArtist;
        }

        public void CorrectWatermark()
        {
            if (string.IsNullOrEmpty(Watermark) && !string.IsNullOrEmpty(OtherFace.Watermark))
            {
                Logger.Trace(namedLogSource, $"Manually correcting missing watermark to match {OtherFace.DisplayName}'s {OtherFace.Watermark} watermark.");
                Watermark = OtherFace.Watermark;
            }
        }

        public bool ValidateCard()
        {
            if (string.IsNullOrEmpty(Name))
            {
                Logger.Warn(LogSource, $"Card is missing name!");
                return false;
            }

            if (NeedsColorOverride && Color.Length != ColorCount)
            {
                Logger.Warn(namedLogSource, $"Colors and color count are mismatched: {Color}, {ColorCount}.");
                return false;
            }

            if (NeedsWatermarkOverride && string.IsNullOrEmpty(Watermark))
            {
                Logger.Warn(namedLogSource, $"Watermark is missing.");
                return false;
            }

            if (NeedsArtistOverride && string.IsNullOrEmpty(Artist))
            {
                Logger.Warn(namedLogSource, $"Artist is missing.");
                return false;
            }

            if (NeedsArtOverride && string.IsNullOrEmpty(ArtFileName))
            {
                Logger.Warn(namedLogSource, $"Art file is missing.");
                return false;
            }

            Logger.Trace(namedLogSource, $"{DisplayName} validated succesfully. No issues detected.");

            return true;
        }
    }
}
