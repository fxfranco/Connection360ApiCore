using Connection360.Domain.Entities;

namespace Connection360.Application.DTOs
{
    public class ClientSummaryResponse
    {
        public Int64 TotalClientRecords { get; set; }
        public Int64 TotalImports { get; set; }
        public Int64 TotalExports { get; set; }
        public Int64 TotalAirShipments { get; init; }
        public Int64 TotalOceanShipments { get; init; }
        public Int64 TotalWithIssues { get; init; }
        public List<ResumenClienteResponse> RecentShipments { get; set; } = new();
    }
}
