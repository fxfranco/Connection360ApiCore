using System.Text.Json.Serialization;

namespace Connection360.Infrastructure.ExternalApi.DTOs
{
    // Mapea EXACTAMENTE la forma de la respuesta del API externo
    public class ExternalApiResponseDto
    {
        [JsonPropertyName("requestedFields")]
        public List<String> RequestedFields { get; set; } = new();

        [JsonPropertyName("missingColumns")]
        public List<String> MissingColumns { get; set; } = new();

        [JsonPropertyName("rows")]
        public List<Dictionary<String, Object?>> Rows { get; set; } = new();
    }
}
