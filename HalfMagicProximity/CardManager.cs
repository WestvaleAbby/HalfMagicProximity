using System.Text.Json;

namespace HalfMagicProximity
{
    public class CardManager
    {
        public void ParseJson(string jsonPath)
        {
            // ARGTODO: Dummy proofing?
            using (Stream jsonStream = new FileStream(jsonPath, FileMode.Open, FileAccess.Read))
            using (JsonDocument jsonDoc = JsonDocument.Parse(jsonStream))
            {
                // An array with all of the parsed card data
                JsonElement scryfallData = jsonDoc.RootElement;

                Logger.Info($"JSON Parsed. Found {scryfallData.GetArrayLength()} cards.");

                CardData testCard = new CardData(
                    scryfallData[0].GetProperty("name").ToString(),
                    scryfallData[0].GetProperty("mana_cost").ToString(),
                    CardFace.Front,
                    CardLayout.Split);

                Logger.Debug(testCard.GetDisplayString());
            }
        }
    }
}
