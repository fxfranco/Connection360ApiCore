namespace Connection360.Api.Models
{
    public class ApiResponse<T>
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Int32 Status { get; set; }
        public String? Error { get; set; }
        public String? Message { get; set; }
        public T? DataResponse { get; set; }
        public MetaResponse? Meta { get; set; }
        public String? Path { get; set; }
    }
}
