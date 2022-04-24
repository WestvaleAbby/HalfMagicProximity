namespace HalfMagicProximity
{
    public enum CardFace { Front, Back };
    public enum CardLayout { Split, Adventure, None };

    public class CardData
    {
        public string Name { get; private set; }
        public string Color { get; private set; }
        public int ColorCount { get; private set; }
        public string ArtFileName { get; private set; }
        public string Artist { get; private set; }
        public CardFace Face { get; private set; }
        public CardLayout Layout { get; private set; }
        public CardData OtherFace { private get; set; }

        public bool NeedsColorOverride => Color != OtherFace.Color;
        public bool NeedsArtistOverride => Artist != OtherFace.Artist;

        public CardData(string name, string manaCost, string art, string artist, CardFace face, CardLayout layout)
        {
            // ARGTODO: These should probably throw exceptions
            if (string.IsNullOrEmpty(name)) Logger.Warn("Card object created with no name!");
            if (string.IsNullOrEmpty(manaCost)) Logger.Warn("Card object created with no manaCost!");
            if (string.IsNullOrEmpty(art)) Logger.Warn("Card object created with no art file name!");
            if (string.IsNullOrEmpty(artist)) Logger.Warn("Card object created with no artist name!");
            if (layout == CardLayout.None) Logger.Warn("Card object created with no layout!");

            Name = name;
            GetColorData(manaCost);
            ArtFileName = art;
            Artist = artist;
            Face = face;
            Layout = layout;
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
                case "UG": return "GU";
                case "WG": return "GW";
                case "WR": return "RW";
                default: return color;
            }
        }

        public string DisplayName => $"{Name} ({Layout} {Face})";
        public string DisplayInfo => $"{DisplayName} | {Color} ({ColorCount} colors) | Artist: {Artist} | Art: '{ArtFileName}'";
    }
}
