using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Connection360.Application.DTOs
{
    public class ClientSummaryResponse
    {
        public Int64 TotalClientRecords { get; set; }
        public Int64 TotalImports { get; set; }
        public Int64 TotalExports { get; set; }
        public Int64 TotalAirShipments { get; init; }
        public Int64 TotalOceanShipments { get; init; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Int64? TotalWithIssues { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ResumenClienteResponse>? RecentShipments { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ResumeMyShipmentsResponse>? MyShipments { get; set; }
    }
}
