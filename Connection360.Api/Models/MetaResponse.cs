namespace Connection360.Api.Models
{
    public class MetaResponse
    {
        public Int64 TotalItems { get; set; }
        public Int64 TotalPages { get; set; }
        public Int64 CurrentPage { get; set; }
        public Int64 Limit { get; set; }
    }
}
