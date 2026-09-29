using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Connection360.Etl.Infrastructure.ExternalApi.DTOs
{
    // Mapea EXACTAMENTE la forma de la respuesta del API externo.
    // Copiado de Connection360.Infrastructure.ExternalApi.DTOs.ExternalApiResponseDto.
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
