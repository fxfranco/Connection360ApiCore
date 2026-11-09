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
            // Se descartan entradas vacías: al configurar por variables de entorno (Cors__AllowedOrigins__0, __1, ...)
            // los espacios sin usar llegan como cadena vacía, que no es un origen válido.
            String[] allowedOrigins = (configuration.GetSection("Cors:AllowedOrigins").Get<String[]>() ?? [])
                .Where(origin => !String.IsNullOrWhiteSpace(origin))
                .ToArray();

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
