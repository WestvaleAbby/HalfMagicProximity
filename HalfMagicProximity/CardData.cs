namespace HalfMagicProximity
{
    public enum CardFace { Front, Back, };

    public class CardData
    {
        public string Name { get; private set; }
        public string Color { get; private set; }
        public int ColorCount { get; private set; }
        public string ArtFileName { get; private set; }
        public CardFace Face { get; private set; }

        public CardData(string name, string manaCost, string art, CardFace face)
        {
            if (string.IsNullOrEmpty(name)) Logger.Error("Card object created with no name");
            if (string.IsNullOrEmpty(manaCost)) Logger.Error("Card object created with no manaCost");
            if (string.IsNullOrEmpty(art)) Logger.Error("Card object created with no art file name");

            Name = name;
            GetColorData(manaCost);
            Face = face;
            ArtFileName = art;
        }

        /// <summary>
        /// Parses the mana cost of a card to determine its colors and color count
        /// </summary>
        /// <param name="manaCost"> The card's mana cost as a string</param>
        private void GetColorData(string manaCost)
        {
            manaCost = manaCost.ToLower();

            char[] colors = { 'w', 'u', 'b', 'r', 'g' };

            foreach (char color in colors)
            {
                if (manaCost.Contains(color))
                {
                    Color += color;
                }
            }

            ColorCount = Color.Length;
        }

        public string GetDisplayString()
        {
            return $"{Name} ({Face})\n - {Color} ({ColorCount} color{(ColorCount > 1 ? "s" : "")})\n - {ArtFileName}";
        }
    }
}
