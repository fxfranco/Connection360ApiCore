using System.Text.Json.Serialization;

namespace Connection360.Domain.Dtos
{
    public class HistoryShipmentsDomainDtoResult
    {
        public List<DetailsHistoryShipmentsDomainDtoResult>? DetailsHistoryShipments { get; set; }
    }
}
