namespace Connection360.ApiGateway.Configuration
{
    /// <summary>
    /// POCO fuertemente tipado para la seccion "Jwt" de appsettings (evita "magic strings").
    /// </summary>
    public sealed class JwtSettings
    {
        public const String SectionName = "Jwt";
        public String Issuer { get; init; } = default!;
        public String Audience { get; init; } = default!;
        public String Secret { get; init; } = default!;
        public Int16 AccessTokenMinutes { get; init; } = 15;
        public Int16 RefreshTokenDays { get; init; } = 7;
    }
}
