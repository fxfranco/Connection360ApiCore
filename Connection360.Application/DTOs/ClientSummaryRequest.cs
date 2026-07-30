namespace Connection360.Application.DTOs
{
    public class ClientSummaryRequest
    {
        public String IdClient { get; set; } = default!;
        public String RolName { get; set; } = default!;
        public String FilterValue { get; set; } = default!;
    }
}
