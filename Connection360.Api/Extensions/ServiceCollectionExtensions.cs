using Asp.Versioning;
using Connection360.Application.Ports;
using Connection360.Application.UseCases;
using Connection360.Domain.Interfaces;
using Connection360.Domain.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace Connection360.Api.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Application
            services.AddScoped<IGetClientSummaryUseCase, GetClientSummaryUseCase>();
            services.AddScoped<IClientSummaryDomainService, ClientSummaryDomainService>();
            return services;
        }

        public static IServiceCollection AddApiVersioningSetup(this IServiceCollection services)
        {
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            }).AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });
            return services;
        }

        /// <summary>
        /// Este servicio actua tambien como Resource Server: valida el mismo JWT que
        /// emite/valida el API Gateway (defensa en profundidad - Zero Trust interno).
        /// La clave/emisor deben coincidir con la configuracion del Gateway.
        /// </summary>
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSection = configuration.GetSection("Jwt");
            var secretKey = jwtSection["Secret"]
                ?? throw new InvalidOperationException("Falta configurar Jwt:Secret");

            var roles = jwtSection["Roles"]
                ?? throw new InvalidOperationException("Falta configurar Jwt:Role");

            //services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            //.AddJwtBearer(options =>
            //{
            //    options.RequireHttpsMetadata = true; // OWASP: nunca aceptar tokens sobre HTTP en produccion
            //    options.SaveToken = false; // no persistir el token innecesariamente
            //    options.TokenValidationParameters = new TokenValidationParameters
            //    {
            //        ValidateIssuer = true,
            //        ValidIssuer = jwtSection["Issuer"],
            //        ValidateAudience = true,
            //        ValidAudience = jwtSection["Audience"],
            //        ValidateIssuerSigningKey = true,
            //        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secretKey)),
            //        ValidateLifetime = true,
            //        ClockSkew = TimeSpan.FromSeconds(30), // margen minimo, no los 5 min por defecto
            //        RoleClaimType = role,
            //        NameClaimType = ClaimTypes.NameIdentifier
            //    };
            //});

            //1.Add Authentication Services

           services.AddAuthentication(options =>
           {
               options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
               options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
           }).AddJwtBearer(options =>
           {
               options.Authority = jwtSection["Issuer"];
               options.Audience = jwtSection["Audience"];
               options.TokenValidationParameters = new TokenValidationParameters
               {
                   // Mapea el claim de Auth0 con el sistema de Roles de ASP.NET Core
                   RoleClaimType = roles,
                   NameClaimType = ClaimTypes.NameIdentifier
               };
           });

            //services.AddAuthorizationBuilder()
            //    .AddPolicy("AdminOnly", p => p.RequireRole("Admin"));

            return services;
        }
    }
}
