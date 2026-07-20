namespace Connection360.ApiGateway.Configuration
{
    public sealed class RateLimitSettings
    {
        public const String SectionName = "RateLimiting";
        public Int16 PermitLimit { get; init; } = 100;
        public Int16 WindowSeconds { get; init; } = 60;
        public Int16 QueueLimit { get; init; } = 0;
    }
}
