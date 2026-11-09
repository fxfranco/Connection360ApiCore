using System.Text.Json.Serialization;

namespace Connection360.Infrastructure.ExternalApi.DTOs
{
    public class ExternalApiOpenStreetMapResponse
    {
        [JsonPropertyName("display_name")]
        public String DisplayName { get; set; } = String.Empty;

        [JsonPropertyName("lat")]
        public String Lat { get; set; } = String.Empty;

        [JsonPropertyName("lon")]
        public String Lon { get; set; } = String.Empty;
    }
}
