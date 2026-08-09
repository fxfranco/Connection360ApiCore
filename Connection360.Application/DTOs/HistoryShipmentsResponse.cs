using System.Text.Json.Serialization;

namespace Connection360.Application.DTOs
{
    public class HistoryShipmentsResponse
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<DetailsHistoryShipmentsResponse>? DetailsHistoryShipments { get; set; }
    }
}
