namespace HalfMagicProximity
{
    public enum CardFace { Front, Back };
    public enum CardLayout { Split, Adventure, None };

    public class CardData
    {
        private const string LogSource = "Card";

        public string Name { get; private set; }
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
        public bool NeedsWatermarkOverride => !string.IsNullOrEmpty(Watermark);

        public CardData(string name, string manaCost, string art, string artist, CardFace face, CardLayout layout, string watermark)
        {
            if (string.IsNullOrEmpty(name)) 
                Logger.Warn("CardData", "Card object created with no name!");
            Name = name;

            if (string.IsNullOrEmpty(manaCost)) 
                Logger.Warn(LogSource + $": {Name}", "Card object created with no manaCost!");
            GetColorData(manaCost);

            if (string.IsNullOrEmpty(art)) 
                Logger.Warn(LogSource + $": {Name}", "Card object created with no art file name!");
            ArtFileName = art;

            if (string.IsNullOrEmpty(artist)) 
                Logger.Warn(LogSource + $": {Name}", "Card object created with no artist name!");
            Artist = artist;

            if (layout == CardLayout.None) 
                Logger.Warn(LogSource + $": {Name}", "Card object created with no layout!");
            Layout = layout;

            Face = face;

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
                    Logger.Debug(LogSource + $": {Name}", $"Correcting color of '{DisplayName}' from UG to GU.");
                    return "GU";
                case "WG":
                    Logger.Debug(LogSource + $": {Name}", $"Correcting color of '{DisplayName}' from WG to GW.");
                    return "GW";
                case "WR":
                    Logger.Debug(LogSource + $": {Name}", $"Correcting color of '{DisplayName}' from WR to RW.");
                    return "RW";
                default: 
                    return color;
            }
        }
        public void CorrectArtist(string newArtist)
        {
            Logger.Debug(LogSource + $": {Name}", $"Manually correcting artist of '{DisplayName}' from '{Artist}' to '{newArtist}'.");
            manualArtist = true;
            Artist = newArtist;
        }

        public string DisplayName => $"{Name} ({Layout} {Face})";
        public string DisplayInfo => $"{DisplayName} | {Color} ({ColorCount} colors) | Artist: {Artist} | Art: '{ArtFileName}'";
    }
}
