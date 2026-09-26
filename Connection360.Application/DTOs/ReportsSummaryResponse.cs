using System.Text.Json.Serialization;

namespace Connection360.Application.DTOs
{
    /// <summary>
    /// Resumen estadístico de envíos de un cliente para la sección de reportes, devuelto por
    /// <c>ReportsController.GetReportTotals</c>.
    /// </summary>
    public class ReportsSummaryResponse
    {
        /// <summary>NIT del cliente.</summary>
        public String ClientNit { get; set; } = String.Empty;

        /// <summary>Nombre del cliente.</summary>
        public String ClientName { get; set; } = String.Empty;

        /// <summary>Cantidad total de envíos registrados del cliente.</summary>
        public Int64 TotalClientRecords { get; set; }

        /// <summary>Cantidad de envíos con novedades.</summary>
        public Int64 TotalWithIssuesStatus { get; init; }

        /// <summary>Cantidad de envíos entregados.</summary>
        public Int64 TotalDeliveredStatus { get; set; }

        /// <summary>Cantidad de envíos en aduana de destino.</summary>
        public Int64 TotalDestinationCustomsStatus { get; set; }

        /// <summary>Cantidad de envíos en aduana de origen.</summary>
        public Int64 TotalOriginCustomsStatus { get; set; }

        /// <summary>Cantidad de envíos en tránsito.</summary>
        public Int64 TotalInTransitStatus { get; set; }

        /// <summary>Cantidad de envíos pendientes.</summary>
        public Int64 TotalPendingStatus { get; set; }

        /// <summary>Valor total facturado.</summary>
        public Double TotalInvoiced { get; init; }

        /// <summary>Valor total de anticipos.</summary>
        public Double TotalAdvancePayment { get; init; }

        /// <summary>Valor total de demoras/atrasos.</summary>
        public Double TotalDelays { get; init; }

        /// <summary>Cantidad total de envíos de importación.</summary>
        public Int64 TotalImports { get; set; }

        /// <summary>Cantidad total de envíos de exportación.</summary>
        public Int64 TotalExports { get; set; }

        /// <summary>Cantidad total de envíos por modalidad aérea.</summary>
        public Int64 TotalAirShipments { get; init; }

        /// <summary>Cantidad total de envíos por modalidad marítima.</summary>
        public Int64 TotalOceanShipments { get; init; }

        /// <summary>Rutas (origen-destino) más frecuentes del cliente. Se omite del JSON cuando es <c>null</c>.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ReportsFrequentRoutesResponse>? FrequentRoutes { get; set; }

    }
}
