using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Connection360.Application.DTOs
{
    /// <summary>
    /// Resumen (totales) de los envíos de un cliente, devuelto por el endpoint de totales del home
    /// (<c>HomeController.GetHomeTotals</c>) y anidado dentro de <see cref="MyShipmentsResponse"/>.
    /// </summary>
    public class ClientSummaryResponse
    {
        /// <summary>Cantidad total de envíos registrados del cliente.</summary>
        public Int64 TotalClientRecords { get; set; }

        /// <summary>Cantidad total de envíos de importación.</summary>
        public Int64 TotalImports { get; set; }

        /// <summary>Cantidad total de envíos de exportación.</summary>
        public Int64 TotalExports { get; set; }

        /// <summary>Cantidad total de envíos por modalidad aérea.</summary>
        public Int64 TotalAirShipments { get; init; }

        /// <summary>Cantidad total de envíos por modalidad marítima.</summary>
        public Int64 TotalOceanShipments { get; init; }

        /// <summary>Cantidad de envíos con novedades. Se omite del JSON cuando es <c>null</c>.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Int64? TotalWithIssues { get; init; }

        /// <summary>Envíos recientes del cliente. Se omite del JSON cuando es <c>null</c>.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ResumenClienteResponse>? RecentShipments { get; set; }

        /// <summary>Listado resumido de "mis envíos". Se omite del JSON cuando es <c>null</c>.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ResumeMyShipmentsResponse>? MyShipments { get; set; }
    }
}
