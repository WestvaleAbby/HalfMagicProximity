using System.Text.Json;

namespace HalfMagicProximity
{
    public class CardManager
    {
        public List<CardData> Cards { get; } = new List<CardData>();

        public void ParseJson(string jsonPath)
        {
            try
            {
                using (Stream jsonStream = new FileStream(jsonPath, FileMode.Open, FileAccess.Read))
                using (JsonDocument jsonDoc = JsonDocument.Parse(jsonStream))
                {
                    JsonElement root = jsonDoc.RootElement;

                    if (root.GetArrayLength() == 0)
                    {
                        Logger.Error($"No cards found in the Scryfall JSON: {jsonPath}");
                        return;
                    }

                    Logger.Debug($"Filtering {root.GetArrayLength()} cards.");
                    for (int i = 0; i < root.GetArrayLength(); i++)
                    {
                        JsonElement node = root[i];

                        // Filter cards not in the Adventure or Split layout
                        string layout = GetCardProperty(node, CardProperty.Layout);
                        if (layout != "adventure" && layout != "split") continue;

                        // Filter out non black bordered cards
                        if (GetCardProperty(node, CardProperty.BorderColor) != "black") continue;

                        // Filter out cards from illegal sets                        
                        bool illegalSetCode = false;
                        foreach (string bannedCode in ConfigManager.IllegalSetCodes)
                        {
                            if (GetCardProperty(node, CardProperty.SetCode).Contains(bannedCode))
                            {
                                illegalSetCode = true;
                                break;
                            }
                        }
                        if (illegalSetCode) continue;

                        // This card is legal, add it to the list
                        AddCard(node);
                    }
                }

                if (Cards.Count == 0)
                    Logger.Error("No legal cards found!");
                else
                    Logger.Info($"There are {Cards.Count} legal cards.");
            }
            catch (FileNotFoundException e)
            {
                Logger.Error($"JSON file not found: {e.Message}");
            }
            catch (JsonException e)
            {
                Logger.Error($"JSON Exception: {e.Message}");
            }
            catch (Exception e)
            {
                Logger.Error($"Error parsing JSON: {e.Message}");
            }
        }

        private void AddCard(JsonElement jsonCard)
        {
            string name = GetCardProperty(jsonCard, CardProperty.Name);
            CardLayout layout = GetCardLayout(GetCardProperty(jsonCard, CardProperty.Layout));

            JsonElement jsonFaces = jsonCard.GetProperty("card_faces");

            CardData[] cardFaces = new CardData[2];

            for (int i = 0; i < jsonFaces.GetArrayLength(); i++)
            {
                CardFace face = (i == 0 ? CardFace.Front : CardFace.Back);
                string artist = GetCardProperty(jsonFaces[i], CardProperty.Artist);

                cardFaces[i] = new CardData(
                    name,
                    GetCardProperty(jsonFaces[i], CardProperty.ManaCost),
                    GenerateArtFileName(name, face, artist),
                    artist,
                    face,
                    layout);

                Cards.Add(cardFaces[i]);
                Logger.Debug($"Added {cardFaces[i].DisplayInfo}");
            }

            cardFaces[0].OtherFace = cardFaces[1];
            cardFaces[1].OtherFace = cardFaces[0];

            if (cardFaces[0].NeedsColorOverride) Logger.Debug($"{cardFaces[0].Name} needs a color override: Front is {cardFaces[0].Color}, Back is {cardFaces[1].Color}.");
            if (cardFaces[0].NeedsArtistOverride) Logger.Debug($"{cardFaces[0].Name} needs an artist override: Front is '{cardFaces[0].Artist}', Back is '{cardFaces[1].Artist}'.");
        }

        private string GenerateArtFileName(string name, CardFace face, string artist)
        {
            if (face == CardFace.Front)
            {
                return $"{name.Replace("/", "")} ({artist}){ConfigManager.ArtFileExtension}";
            }
            else
            {
                string[] nameSubstrings = name.Split('/');
                return nameSubstrings[nameSubstrings.Length - 1].Replace(" ", "").ToLower() + ConfigManager.ArtFileExtension;
            }
        }

        // Extract the value of a json element's property as a string
        private string GetCardProperty(JsonElement element, CardProperty property)
        {
            string stringProperty = element.GetProperty(PropertyString(property)).ToString();

            if (string.IsNullOrEmpty(stringProperty)) Logger.Error($"Card JSON missing {property} value.");

            return stringProperty;
        }

        // Enum to facilitate accessing specific json card properties. Maintain alphabetization
        private enum CardProperty
        {
            Artist,
            BorderColor,
            Layout,
            ManaCost,
            Name,
            SetCode,
        };

        // Converts CardProperty enum to the string found in the scryfall json
        private string PropertyString(CardProperty property)
        {
            switch (property)
            {
                case CardProperty.Name: return "name";
                case CardProperty.ManaCost: return "mana_cost";
                case CardProperty.Layout: return "layout";
                case CardProperty.Artist: return "artist";
                case CardProperty.BorderColor: return "border_color";
                case CardProperty.SetCode: return "set";
                default:
                    Logger.Error($"Tried to access a card property that doesn't exist: {property}");
                    return "";
            }
        }

        private CardLayout GetCardLayout(string layout)
        {
            switch (layout)
            {
                case "split": return CardLayout.Split;
                case "adventure": return CardLayout.Adventure;
                default:
                    Logger.Error($"Card is missing its layout!");
                    return CardLayout.None;
            }
        }
    }
}
