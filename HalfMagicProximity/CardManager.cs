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

                        // Filter out reprints, so we can properly grab watermarks
                        if (GetBooleanCardProperty(node, CardProperty.Reprint)) continue;

                        // Filter cards not in the Adventure or Split layout
                        string layout = GetCardProperty(node, CardProperty.Layout);
                        if (layout != "adventure" && layout != "split") continue;

                        // Filter out various duplicates
                        if (GetBooleanCardProperty(node, CardProperty.Promo)) continue;
                        if (GetBooleanCardProperty(node, CardProperty.Variation)) continue;

                        try
                        {
                            JsonElement frameEffects = node.GetProperty(PropertyString(CardProperty.FrameEffects));

                            if (frameEffects.GetArrayLength() > 0)
                                continue;
                        }
                        catch
                        {
                            // Card does not have frame effects. No need to act.
                            ;
                        }

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

            // Only add cards in the debug card list if we're using that subset of cards
            if (ConfigManager.UseDebugCardSubset && !ConfigManager.DebugCards.Contains(name.ToLower())) return;

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
                    layout,
                    GetCardProperty(jsonFaces[i], CardProperty.Watermark));

                // Check for manual artist overrides and implement them
                ManualArtistOverride manualArtistOverride = CheckForManualArtistOverride(name, face);
                if (manualArtistOverride != null && face == manualArtistOverride.CardFace)
                    cardFaces[i].CorrectArtist(manualArtistOverride.Artist);

                if (cardFaces[i].NeedsWatermarkOverride)
                    Logger.Debug($"'{cardFaces[i].Name}' needs a watermark override for {cardFaces[i].Watermark}.");

                Cards.Add(cardFaces[i]);
                Logger.Debug($"Added {cardFaces[i].DisplayInfo}");
            }

            cardFaces[0].OtherFace = cardFaces[1];
            cardFaces[1].OtherFace = cardFaces[0];

            if (cardFaces[0].NeedsColorOverride)
                Logger.Debug($"'{cardFaces[0].Name}' needs a color override: Front is {cardFaces[0].Color}, Back is {cardFaces[1].Color}.");

            if (cardFaces[0].NeedsArtistOverride)
            {
                string frontArtist = cardFaces[0].Artist;
                string backArtist = cardFaces[1].Artist;

                if (frontArtist == backArtist)
                    Logger.Debug($"'{cardFaces[0].Name}' needs an artist override since it was manually corrected.");
                else
                    Logger.Debug($"'{cardFaces[0].Name}' needs an artist override: Front is '{cardFaces[0].Artist}', Back is '{cardFaces[1].Artist}'.");
            }
        }

        private ManualArtistOverride CheckForManualArtistOverride(string name, CardFace face)
        {
            foreach (ManualArtistOverride artistOverride in ConfigManager.ManualArtistOverrides)
            {
                if (artistOverride.CardName == name.ToLower() && artistOverride.CardFace == face)
                    return artistOverride;
            }

            return null;
        }

        private string GenerateArtFileName(string name, CardFace face, string artist)
        {
            if (face == CardFace.Front)
            {
                return name.Replace("/", "") + ConfigManager.ArtFileExtension;
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
            string stringProperty = "";
            try
            {
                stringProperty = element.GetProperty(PropertyString(property)).ToString();
            }
            catch (KeyNotFoundException e)
            {
                if (property != CardProperty.Watermark)
                    Logger.Error($"Card JSON missing {property} value: {e.Message}");
            }

            return stringProperty;
        }

        private bool GetBooleanCardProperty(JsonElement element, CardProperty property)
        {
            bool propertyValue = false;
            try
            {
                propertyValue = element.GetProperty(PropertyString(property)).GetBoolean();
            }
            catch (KeyNotFoundException e)
            {
                if (property != CardProperty.Watermark)
                    Logger.Error($"Card JSON missing {property} value: {e.Message}");
            }

            return propertyValue;
        }

        // Enum to facilitate accessing specific json card properties. Maintain alphabetization please!
        private enum CardProperty
        {
            Artist,
            BorderColor,
            FrameEffects,
            Layout,
            ManaCost,
            Name,
            Promo,
            Reprint,
            SetCode,
            Watermark,
            Variation,
        };

        // Converts CardProperty enum to the string found in the scryfall json
        private string PropertyString(CardProperty property)
        {
            // Maintain alphabetization please!
            switch (property)
            {
                case CardProperty.Artist: return "artist";
                case CardProperty.BorderColor: return "border_color";
                case CardProperty.FrameEffects: return "frame_effects";
                case CardProperty.Layout: return "layout";
                case CardProperty.ManaCost: return "mana_cost";
                case CardProperty.Name: return "name";
                case CardProperty.Promo: return "promo";
                case CardProperty.Reprint: return "reprint";
                case CardProperty.SetCode: return "set";
                case CardProperty.Watermark: return "watermark";
                case CardProperty.Variation: return "variation";
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
