using Connection360.ApiGateway.Configuration;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace Connection360.ApiGateway.Extensions
{
    /// <summary>
    /// Rate limiting nativo de ASP.NET Core (System.Threading.RateLimiting).
    /// Mitiga OWASP API4:2023 - Unrestricted Resource Consumption y ataques de fuerza bruta / DoS.
    /// Particiona por IP del cliente: cada IP tiene su propia ventana de peticiones.
    /// </summary>
    public static class RateLimitingExtensions
    {
        public static IServiceCollection AddGatewayRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            var settings = configuration.GetSection(RateLimitSettings.SectionName).Get<RateLimitSettings>()
                ?? new RateLimitSettings();

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, String>(httpContext =>
                {
                    var clientKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(clientKey, _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = settings.QueueLimit
                    });
                });

                // Politica mas estricta especifica para el endpoint de login/token (anti brute-force)
                options.AddFixedWindowLimiter("AuthPolicy", opt =>
                {
                    opt.PermitLimit = 5;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 0;
                });

                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.ContentType = "application/json";
                    await context.HttpContext.Response.WriteAsync(
                        "{\"error\":\"Demasiadas solicitudes. Intente nuevamente mas tarde.\"}", cancellationToken);
                };
            });

            return services;
        }
    }
}
