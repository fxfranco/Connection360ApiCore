namespace Connection360.Domain.Interfaces
{
    public class ResolveClientAccessRequest
    {
        public String IdClient { get; set; } = default!;
        public String RoleName { get; set; } = default!;
        public String FilterValue { get; set; } = default!;
        public String IdQueryClient { get; set; } = default!;
        public Boolean AllClient { get; set; } = default!;
    }
}
