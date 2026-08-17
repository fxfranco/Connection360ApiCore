namespace Connection360.Application.DTOs
{
    public class ReportsFrequentRoutesResponse
    {
        public String Origin {  get; set; } = String.Empty;
        public String Destination {  get; set; } = String.Empty;
        public Int64 TotalRoute {  get; set; }
    }
}
