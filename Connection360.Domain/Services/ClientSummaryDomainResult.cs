using Connection360.Domain.Entities;

namespace Connection360.Domain.Services
{
    public class ClientSummaryDomainResult
    {
        public Int64 TotalClientRecords { get; init; }
        public Int64 TotalImports { get; init; }
        public Int64 TotalExports { get; init; }
        public Int64 TotalAirShipments { get; init; }
        public Int64 TotalOceanShipments { get; init; }
        public Int64 TotalWithIssues { get; init; }
        public List<ResumenClienteDto> RecentShipments { get; init; } = new List<ResumenClienteDto>();
    }
}
