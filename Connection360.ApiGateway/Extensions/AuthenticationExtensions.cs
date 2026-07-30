using Connection360.ApiGateway.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Connection360.ApiGateway.Extensions
{
    /// <summary>
    /// Configura JWT Bearer (para validar tokens propios) y deja el punto de extension
    /// para integrar un proveedor OAuth 2.0 externo (Azure AD, Auth0, IdentityServer/Duende, Keycloak)
    /// simplemente agregando ValidIssuer/ValidAudience/Authority del proveedor real.
    /// </summary>
    public static class AuthenticationExtensions
    {
        public static IServiceCollection AddGatewayAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                ?? throw new InvalidOperationException("Seccion Jwt no configurada.");

            services.AddSingleton(jwtSettings);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // Para OAuth2/OIDC con proveedor externo, reemplazar por:
                options.Authority = jwtSettings.Issuer;
                options.Audience = jwtSettings.Audience;

                //options.RequireHttpsMetadata = true; // OWASP: nunca validar tokens sobre HTTP en produccion
                //options.SaveToken = false;
                //options.TokenValidationParameters = new TokenValidationParameters
                //{
                //    ValidateIssuer = true,
                //    ValidIssuer = jwtSettings.Issuer,
                //    ValidateAudience = true,
                //    ValidAudience = jwtSettings.Audience,
                //    ValidateIssuerSigningKey = true,
                //    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                //    ValidateLifetime = true,
                //    ClockSkew = TimeSpan.FromSeconds(30),
                //    RequireExpirationTime = true
                //};

                // Evita filtrar detalles del error de autenticacion al cliente (OWASP A09)
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        context.NoResult();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        return context.Response.WriteAsync("{\"error\":\"Token invalido o expirado.\"}");
                    },
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        if (!context.Response.HasStarted)
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            context.Response.ContentType = "application/json";
                            return context.Response.WriteAsync("{\"error\":\"No autenticado.\"}");
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            services.AddAuthorization();

            return services;
        }
    }
}
