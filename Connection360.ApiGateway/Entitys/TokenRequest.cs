namespace Connection360.ApiGateway.Entitys
{
    public sealed record class TokenRequest
    {
        public String GrantType { get; init; } = String.Empty;
        public String? ClientId { get; init; }
        public String? ClientSecret { get; init; }
    }
}
