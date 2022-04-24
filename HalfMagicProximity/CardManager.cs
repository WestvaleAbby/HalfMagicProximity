using System.Text.Json;

namespace HalfMagicProximity
{
    public class CardManager
    {
        // ARGTODO: Remove once actual card storage is implemented
        private int cardCount = 0;

        public void ParseJson(string jsonPath)
        {
            // ARGTODO: Dummy proofing?
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
                    string layout = GetProperty(node, CardProperty.Layout);
                    if (layout != "adventure" && layout != "split") continue;

                    // Filter out non black bordered cards
                    if (GetProperty(node, CardProperty.BorderColor) != "black") continue;

                    // Filter out cards from banned sets
                    string[] bannedSetCodes =
                    {
                        "cmb", // Playtest Cards
                        "htr", // Heroes of the Realm
                    };
                    bool illegalSetCode = false;
                    foreach (string bannedCode in bannedSetCodes)
                    {
                        if (GetProperty(node, CardProperty.SetCode).Contains(bannedCode))
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

            if (cardCount == 0)
                Logger.Error("No legal cards found!");
            else
                Logger.Info($"There are {cardCount} legal cards.");
        }

        private void AddCard(JsonElement jsonCard)
        {
            // ARGTODO: Pull relevant card data from JSON
            cardCount++;
            Logger.Debug($"{GetProperty(jsonCard, CardProperty.Name)} is legal.");
        }

        // Extract the value of a json element's property as a string
        private string GetProperty(JsonElement element, CardProperty property)
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
    }
}
