namespace HalfMagicProximity
{
    public enum CardFace { Front, Back, };
    public enum CardLayout { Split, Adventure, };

    public class CardData
    {
        public string Name { get; private set; }
        public string Color { get; private set; }
        public int ColorCount { get; private set; }
        public CardFace Face { get; private set; }
        public CardLayout Layout { get; private set; }
        public string ArtPath { get; private set; }

        public CardData(string name, string manaCost, CardFace face, CardLayout layout)
        {
            if (string.IsNullOrEmpty(name)) Logger.Error("Card object created with no name");
            if (string.IsNullOrEmpty(manaCost)) Logger.Error("Card object created with no manaCost");

            Name = name;
            GetColorData(manaCost);
            Face = face;
            Layout = layout;
        }

        /// <summary>
        /// Parses the mana cost of a card to determine its colors and color count. Assigns to those variables directly rather than returning any values
        /// </summary>
        /// <param name="manaCost"> The card's mana cost as a string</param>
        private void GetColorData(string manaCost)
        {
            manaCost = manaCost.ToLower();

            string[] colors = { "w", "u", "b", "r", "g" };

            foreach (string color in colors)
            {
                if (manaCost.Contains(color))
                {
                    Color += color;
                }
            }

            ColorCount = colors.Length;
        }

        public string GetDisplayString()
        {
            return $"1 {Name}";
        }
    }
}
