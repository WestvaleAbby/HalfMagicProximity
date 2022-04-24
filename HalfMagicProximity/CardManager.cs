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

                        // Filter out cards from banned sets
                        string[] bannedSetCodes =
                        {
                        "cmb", // Playtest Cards
                        "htr", // Heroes of the Realm
                    };
                        bool illegalSetCode = false;
                        foreach (string bannedCode in bannedSetCodes)
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
            }
            catch (FileNotFoundException e)
            {
                Logger.Error($"JSON file at {e.FileName} not found: {e.Message}");
            }
            catch (JsonException e)
            {
                Logger.Error($"JSON Exception: {e.Message}");
            }
            catch (Exception e)
            {
                Logger.Error($"Error parsing JSON: {e.Message}");
            }

            if (Cards.Count == 0)
                Logger.Error("No legal cards found!");
            else
                Logger.Info($"There are {Cards.Count} legal cards.");
        }

        private void AddCard(JsonElement jsonCard)
        {
            string name = GetCardProperty(jsonCard, CardProperty.Name);
            CardLayout layout = GetCardLayout(GetCardProperty(jsonCard, CardProperty.Layout));

            JsonElement jsonFaces = jsonCard.GetProperty("card_faces");

            for (int i = 0; i < jsonFaces.GetArrayLength(); i++)
            {
                CardFace face = (i == 0 ? CardFace.Front : CardFace.Back);
                string artist = GetCardProperty(jsonFaces[i], CardProperty.Artist);

                CardData card = new CardData(
                    name,
                    GetCardProperty(jsonFaces[i], CardProperty.ManaCost),
                    ArtFileName(name, face, artist),
                    artist,
                    face,
                    layout);

                Cards.Add(card);
                Logger.Debug($"Added {card.GetDisplayString()}");
            }
        }

        private string ArtFileName(string name, CardFace face, string artist)
        {
            // ARGTODO: Pull from config? Not sure if this ever changes
            const string ART_FILE_EXTENSION = ".jpg";

            if (face == CardFace.Front)
            {
                return $"{name.Replace("/", "")} ({artist}){ART_FILE_EXTENSION}";
            }
            else
            {
                string[] nameSubstrings = name.Split('/');
                return nameSubstrings[nameSubstrings.Length - 1].Replace(" ", "").ToLower() + ART_FILE_EXTENSION;
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
