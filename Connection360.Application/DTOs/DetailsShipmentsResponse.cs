using System.Text.Json.Serialization;

namespace Connection360.Application.DTOs
{
    public class DetailsShipmentsResponse
    {
        /// <summary>
        /// Resumen de la información de los envios
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SummaryShipmentsResponse? ResumenShipments { get; set; }

        /// <summary>
        /// Seguimiento de la información de los envios
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TrackingShipmentsResponse? TrackingShipments { get; set; }

        /// <summary>
        /// Fechas logisticas  de la información de los envios
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public LogisticsDatesShipmentsResponse? LogisticsDatesShipments { get; set; }

        /// <summary>
        /// Información de los contenedores de los envios
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ContainerShipmentsResponse? ContainerShipments { get; set; }

        /// <summary>
        /// Información financiera de los envios
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public FinancialInfoShipmentsResponse? FinancialInfoShipments { get; set; }

        /// <summary>
        /// Historial de los envios
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public HistoryShipmentsResponse? HistoryShipments { get; set; }
    }
}
