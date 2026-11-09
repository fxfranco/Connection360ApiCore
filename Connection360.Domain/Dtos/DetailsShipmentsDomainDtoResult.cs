using System.Text.Json.Serialization;

namespace Connection360.Domain.Dtos
{
    public class DetailsShipmentsDomainDtoResult
    {
        /// <summary>
        /// Resumen de la información de los envios
        /// </summary>
        public SummaryShipmentsDomainDtoResult? ResumenShipments { get; set; }

        /// <summary>
        /// Seguimiento de la información de los envios
        /// </summary>
        public TrackingShipmentsDomainDtoResult? TrackingShipments { get; set; }

        /// <summary>
        /// Fechas logisticas  de la información de los envios
        /// </summary>
        public LogisticsDatesShipmentsDomainDtoResult? LogisticsDatesShipments { get; set; }

        /// <summary>
        /// Información de los contenedores de los envios
        /// </summary>
        public ContainerShipmentsDomainDtoResult? ContainerShipments { get; set; }

        /// <summary>
        /// Información financiera de los envios
        /// </summary>
        public FinancialInfoShipmentsDomainDtoResult? FinancialInfoShipments { get; set; }

        /// <summary>
        /// Historial de los envios
        /// </summary>
        public HistoryShipmentsDomainDtoResult? HistoryShipments { get; set; }
    }
}
