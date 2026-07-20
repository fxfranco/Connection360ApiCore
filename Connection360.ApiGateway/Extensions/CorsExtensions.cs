namespace Connection360.ApiGateway.Extensions
{
    /// <summary>
    /// CORS restrictivo por Whitelist (nunca usar AllowAnyOrigin en produccion,
    /// especialmente combinado con AllowCredentials -> vulnerabilidad critica OWASP).
    /// </summary>
    public static class CorsExtensions
    {
        public const String PolicyName = "GatewayCorsPolicy";

        public static IServiceCollection AddGatewayCors(this IServiceCollection services, IConfiguration configuration)
        {
            var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<String[]>() ?? [];

            services.AddCors(options =>
            {
                options.AddPolicy(PolicyName, policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                          .AllowCredentials()
                          .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
                });
            });

            return services;
        }
    }
}
