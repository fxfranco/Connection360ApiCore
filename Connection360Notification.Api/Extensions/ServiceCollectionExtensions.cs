using Asp.Versioning;
using Connection360Notification.Application.Ports;
using Connection360Notification.Application.Ports.Inbound;
using Connection360Notification.Application.Services;
using Connection360Notification.Application.UseCases;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace Connection360Notification.Api.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<INotificationUseCase, NotificationUseCase>();

            //Notifications
            services.AddScoped<IGetNotificationsUseCase, GetNotificationsUseCase>();
            services.AddScoped<IProcessIncomingNotificationUseCase, ProcessIncomingNotificationUseCase>();

            // Sincronización de usuarios básicos (id, identificación, email, teléfono) provenientes de Auth0
            services.AddScoped<ISyncBasicUsersUseCase, SyncBasicUsersUseCase>();

            // Envío por canales (Strategy + Registry). Las implementaciones de INotificationChannel
            // las registra Infrastructure (AddInfrastructure).
            services.AddScoped<INotificationChannelRegistry, NotificationChannelRegistry>();
            services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
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
            _ = jwtSection["Secret"]
                ?? throw new InvalidOperationException("Falta configurar Jwt:Secret");

            var roles = jwtSection["Roles"]
                ?? throw new InvalidOperationException("Falta configurar Jwt:Role");

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

               options.Events = new JwtBearerEvents
               {
                   OnMessageReceived = context =>
                   {
                       var accessToken = context.Request.Query["access_token"];
                       var path = context.HttpContext.Request.Path;

                       // Si la petición va dirigida al Hub de SignalR, extraemos el token de la query string
                       if (!String.IsNullOrEmpty(accessToken) && path.Value?.Contains("/hubs/notifications") == true)
                       {
                           context.Token = accessToken;
                       }
                       return Task.CompletedTask;
                   }
               };
           });

           return services;
        }
    }
}
