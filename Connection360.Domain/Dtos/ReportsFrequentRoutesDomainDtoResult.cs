namespace Connection360.Domain.Dtos
{
    public class ReportsFrequentRoutesDomainDtoResult
    {
        public String Origin { get; set; } = String.Empty;
        public String Destination { get; set; } = String.Empty;
        public Int64 TotalRoute { get; set; }
    }
}
