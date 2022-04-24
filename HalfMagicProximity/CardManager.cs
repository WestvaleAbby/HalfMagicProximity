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

                Console.WriteLine($"JSON Parsed. Found {scryfallData.GetArrayLength()} cards.");
            }
        }
    }
}
