using System.Text.Json.Serialization;

namespace Connection360.Application.DTOs
{
    /// <summary>
    /// Historial de cambios de un envío, expuesto dentro de <see cref="DetailsShipmentsResponse.HistoryShipments"/>.
    /// </summary>
    public class HistoryShipmentsResponse
    {
        /// <summary>Listado cronológico de cambios registrados sobre el envío. Se omite del JSON cuando es <c>null</c>.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<DetailsHistoryShipmentsResponse>? DetailsHistoryShipments { get; set; }
    }
}
