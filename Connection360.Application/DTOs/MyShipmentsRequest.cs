namespace Connection360.Application.DTOs
{
    public class MyShipmentsRequest
    {
        public String IdClient { get; set; } = default!;
        public String RoleName { get; set; } = default!;
        public Int64 Page { get; set; } = default!;
        public Int64 Size { get; set; } = default!;
        public MyShipmentsFiltersRequest? Filters { get; set; }
    }
}
