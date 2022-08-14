using System.Text.Json;

namespace HalfMagicProximity
{
    /// <summary>
    /// CardManager handles parsing of the Scryfall JSON and creates the list of cards to render
    /// </summary>
    public class CardManager
    {
        private const string LogSource = "CardManager";

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
                        Logger.Error(LogSource, $"No cards found in the Scryfall JSON: {jsonPath}");
                        return;
                    }

                    Logger.Info(LogSource, $"Filtering {root.GetArrayLength()} cards. This may take several minutes, please do not close this window!");
                    for (int i = 0; i < root.GetArrayLength(); i++)
                    {
                        JsonElement node = root[i];

                        // Filter cards not in the Adventure or Split layout
                        string layout = GetCardProperty(node, CardProperty.Layout);
                        if (layout != "adventure" && layout != "split") continue;

                        // Filter out various duplicates
                        if (GetBooleanCardProperty(node, CardProperty.Promo)) continue;

                        // Filter out showcases and the like to avoid duplicates
                        // ARGTODO: This may not always work, depending on how future cards are released
                        try
                        {
                            JsonElement frameEffects = node.GetProperty(PropertyString(CardProperty.FrameEffects));

                            if (frameEffects.GetArrayLength() > 0)
                                continue;
                        }
                        catch
                        {
                            // Card does not have frame effects. No need to act.
                        }

                        // Filter out Alchemy cards
                        if (GetCardProperty(node, CardProperty.SetType) == "alchemy") continue;

                        // Filter out non black bordered bordered cards
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

                if (Cards.Count > 0)
                {
                    Logger.Debug(LogSource, $"Found {Cards.Count} legal cards.");

                    // Check that all of the cards in the card subset got used
                    if (ConfigManager.UseCardSubset)
                    {
                        foreach (string cardName in ConfigManager.CardSubset)
                        {
                            CardData cardObject = Cards.Where(x => x.Name.ToLower() == cardName.ToLower()).FirstOrDefault();

                            if (cardObject == null)
                                Logger.Warn(LogSource, $"'{cardName}' from the list of subset cards is not used. Please verify that it is entered correctly.");
                        }
                    }

                    // Check that all artist overrides were used
                    if (ConfigManager.ManualArtistOverrides.Count > 0 && !ConfigManager.UpdatesOnly)
                    {
                        foreach (ManualArtistOverride artistOverride in ConfigManager.ManualArtistOverrides)
                        {
                            CardData cardObject = Cards.FirstOrDefault(x => (x.Name.ToLower() == artistOverride.CardName.ToLower() && x.Face == artistOverride.CardFace));

                            if (cardObject == null)
                                Logger.Warn(LogSource, $"'{artistOverride.CardName} ({artistOverride.CardFace})' from the list of manual artist overrides is not used. Please verify that it is entered correctly.");
                        }
                    }
                }
                else
                {
                    Logger.Error(LogSource, "No legal cards found!");
                }
            }
            catch (FileNotFoundException e)
            {
                Logger.Error(LogSource, $"JSON file not found: {e.Message}");
            }
            catch (JsonException e)
            {
                Logger.Error(LogSource, $"JSON Exception: {e.Message}");
            }
            catch (Exception e)
            {
                Logger.Error(LogSource, $"Error parsing JSON: {e.Message}");
            }
        }

        private void AddCard(JsonElement jsonCard)
        {
            string name = GetCardProperty(jsonCard, CardProperty.Name);
            bool isRepeat = false;

            // Only add cards in the specified card list if we're using that subset of cards
            if (ConfigManager.UseCardSubset && !ConfigManager.CardSubset.Contains(name.ToLower())) return;

            CardLayout layout = GetCardLayout(GetCardProperty(jsonCard, CardProperty.Layout));

            JsonElement jsonFaces = jsonCard.GetProperty("card_faces");

            CardData[] cardFaces = new CardData[2];

            for (int i = 0; i < jsonFaces.GetArrayLength(); i++)
            {
                CardFace face = (i == 0 ? CardFace.Front : CardFace.Back);
                CardTemplate template = CardTemplate.M15;
                if (face == CardFace.Back)
                {
                    if (GetCardProperty(jsonCard, CardProperty.Keywords).ToLower().Contains("aftermath"))
                        template = CardTemplate.DoubleFeature;
                    else if (layout == CardLayout.Adventure)
                        template = CardTemplate.Sketch;
                }

                cardFaces[i] = new CardData(
                    name,
                    GetCardProperty(jsonFaces[i], CardProperty.ManaCost),
                    GenerateArtSourceFileName(name, face),
                    GetCardProperty(jsonFaces[i], CardProperty.Artist),
                    face,
                    layout,
                    template,
                    GetCardProperty(jsonFaces[i], CardProperty.Watermark));
            }

            cardFaces[0].OtherFace = cardFaces[1];
            cardFaces[1].OtherFace = cardFaces[0];

            foreach (CardData card in cardFaces)
            {
                // Check if this is a duplicate and whether this new version has a watermark the old one doesn't
                CardData repeat = Cards.FirstOrDefault(x => x.DisplayName == card.DisplayName);
                if (repeat != null)
                {
                    if (string.IsNullOrEmpty(repeat.Watermark) && !string.IsNullOrEmpty(card.Watermark))
                    {
                        repeat.Watermark = card.Watermark;
                        Logger.Trace(LogSource, $"Found an additional {repeat.Watermark} watermark for {repeat.DisplayName}.");
                    }

                    Logger.Trace(LogSource, $"Found a duplicate entry for {repeat.DisplayName}. Skipping.");
                    continue;
                }

                // Filter out cards that already have art if we're only doing updates
                if (ConfigManager.UpdatesOnly &&
                File.Exists(Path.Combine(ConfigManager.OutputDirectory, card.DisplayName + ".png")) &&
                File.Exists(Path.Combine(ConfigManager.OutputDirectory, card.OtherFace.DisplayName + ".png")))
                {
                    Logger.Trace(LogSource, $"A render already exists for {card.DisplayName}. Skipping.");
                    continue;
                }

                // Check for manual artist overrides and implement them
                ManualArtistOverride manualArtistOverride = CheckForManualArtistOverride(name, card.Face);
                if (manualArtistOverride != null && card.Face == manualArtistOverride.CardFace)
                    card.CorrectArtist(manualArtistOverride.Artist);

                Cards.Add(card);

                Logger.Debug(LogSource, $"{card.DisplayName} is legal.");
                Logger.Trace(LogSource, $" - {card.Name} ({card.Layout} {card.Face})");
                Logger.Trace(LogSource, $" - {card.Color} ({card.ColorCount} colors)");
                Logger.Trace(LogSource, $" - Artist: {card.Artist} | Art: {card.ArtSourceFile}");
                if (!string.IsNullOrEmpty(card.Watermark))
                    Logger.Trace(LogSource, $" - Watermark: {card.Watermark}");

                if (card.NeedsColorOverride)
                    Logger.Trace(LogSource, $"'{card.Name}' needs a color override: Front is {cardFaces[0].Color}, Back is {cardFaces[1].Color}.");

                if (card.NeedsArtistOverride)
                {
                    string frontArtist = cardFaces[0].Artist;
                    string backArtist = cardFaces[1].Artist;

                    if (frontArtist == backArtist)
                        Logger.Trace(LogSource, $"'{card.Name}' needs an artist override since it was manually corrected.");
                    else
                        Logger.Trace(LogSource, $"'{card.Name}' needs an artist override: Front is '{cardFaces[0].Artist}', Back is '{cardFaces[1].Artist}'.");
                }

                // Some cards (some back gold faces of hybrid split cards) don't properly have their watermark in the JSON
                if (card.NeedsWatermarkOverride || card.OtherFace.NeedsWatermarkOverride)
                {
                    if (string.IsNullOrEmpty(cardFaces[0].Watermark))
                        cardFaces[0].CorrectWatermark();
                    if (string.IsNullOrEmpty(cardFaces[1].Watermark))
                        cardFaces[1].CorrectWatermark();

                    Logger.Trace(LogSource, $"'{card.Name}' needs a watermark override: Front is '{cardFaces[0].Watermark}'. Back is '{cardFaces[1].Watermark}'.");
                }
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

        private string GenerateArtSourceFileName(string name, CardFace face)
        {
            string[] nameSubstrings = name.Split('/');
            int index = (face == CardFace.Front ? 0 : nameSubstrings.Length - 1);

            // ARGTODO: Use some regex if these replacements need to be expanded further
            return nameSubstrings[index].Replace(" ", "").Replace("'", "").ToLower() + ConfigManager.ArtFileExtension;
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
                    Logger.Error(LogSource, $"Card JSON missing {property} value: {e.Message}");
            }

            return stringProperty;
        }

        // Extract the value of a json element's property as a bool if able
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
                    Logger.Error(LogSource, $"Card JSON missing {property} value: {e.Message}");
            }

            return propertyValue;
        }

        // Enum to facilitate accessing specific json card properties. Maintain alphabetization please!
        private enum CardProperty
        {
            Artist,
            BorderColor,
            FrameEffects,
            Keywords,
            Layout,
            ManaCost,
            Name,
            //OracleText,
            Promo,
            SetCode,
            SetType,
            Watermark,
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
                case CardProperty.Keywords: return "keywords";
                case CardProperty.Layout: return "layout";
                case CardProperty.ManaCost: return "mana_cost";
                case CardProperty.Name: return "name";
                //case CardProperty.OracleText: return "oracle_text";
                case CardProperty.Promo: return "promo";
                case CardProperty.SetCode: return "set";
                case CardProperty.SetType: return "set_type";
                case CardProperty.Watermark: return "watermark";
                default:
                    Logger.Error(LogSource, $"Tried to access a card property that doesn't exist: {property}");
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
                    Logger.Error(LogSource, $"Card is missing its layout! Defaulting to 'Split'.");
                    return CardLayout.Split;
            }
        }
    }
}
