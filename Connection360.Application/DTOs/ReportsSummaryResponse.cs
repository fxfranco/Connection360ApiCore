using System.Text.Json.Serialization;

namespace Connection360.Application.DTOs
{
    public class ReportsSummaryResponse
    {
        public Int64 TotalClientRecords { get; set; }
        public Int64 TotalWithIssuesStatus { get; init; }
        public Int64 TotalDeliveredStatus { get; set; }
        public Int64 TotalDestinationCustomsStatus { get; set; }
        public Int64 TotalOriginCustomsStatus { get; set; }
        public Int64 TotalInTransitStatus { get; set; }
        public Int64 TotalPendingStatus { get; set; }
        public Double TotalInvoiced { get; init; }
        public Double TotalAdvancePayment { get; init; }
        public Double TotalDelays { get; init; }
        public Int64 TotalImports { get; set; }
        public Int64 TotalExports { get; set; }
        public Int64 TotalAirShipments { get; init; }
        public Int64 TotalOceanShipments { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ReportsFrequentRoutesResponse>? FrequentRoutes { get; set; }

    }
}
