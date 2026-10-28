namespace Connection360.Application.DTOs
{
    /// <summary>
    /// Ruta (origen-destino) frecuente de un cliente, expuesta dentro de
    /// <see cref="ReportsSummaryResponse.FrequentRoutes"/>.
    /// </summary>
    public class ReportsFrequentRoutesResponse
    {
        /// <summary>Lugar de origen de la ruta.</summary>
        public String Origin {  get; set; } = String.Empty;

        /// <summary>Lugar de destino de la ruta.</summary>
        public String Destination {  get; set; } = String.Empty;

        /// <summary>Cantidad de envíos registrados en esta ruta.</summary>
        public Int64 TotalRoute {  get; set; }
    }
}
