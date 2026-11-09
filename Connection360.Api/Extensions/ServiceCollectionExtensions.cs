using Asp.Versioning;
using Connection360.Application.Ports;
using Connection360.Application.Ports.Persistence;
using Connection360.Application.Services;
using Connection360.Application.UseCases;
using Connection360.Application.UseCases.Persistence;
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
            //Home
            services.AddScoped<IGetClientSummaryUseCase, GetClientSummaryUseCase>();
            services.AddScoped<IClientSummaryDomainService, ClientSummaryDomainService>();
            services.AddScoped<IClientAccessResolver, ClientAccessResolver>();
            services.AddScoped<IApplicationDataSheetDataGateway, ApplicationDataSheetDataGateway>();
            services.AddScoped<ILogStatusTrackingDataGateway, LogStatusTrackingDataGateway>();
            services.AddScoped<IClientRecordsFilterService, ClientRecordsFilterService>();
            

            //MyShipments
            services.AddScoped<IGetMyShipmentsUseCase, GetMyShipmentsUseCase>();
            services.AddScoped<IMyShipmentsDomainService, MyShipmentsDomainService>();

            //Detail History Shipments
            services.AddScoped<IDetailsHistoryShipmentsDomainService, DetailsHistoryShipmentsDomainService>();

            //Reports 
            services.AddScoped<IGetReportsUseCase, GetReportsUseCase>();            
            services.AddScoped<IReportsDomainService, ReportsDomainService>();

            //Auth0 UserManagement
            services.AddScoped<IGetUserManagementUseCase, GetUserManagementUseCase>();

            services.AddScoped<IDynamicDataSetMerger, DynamicDataSetMerger>();


            // 4. Servicios de aplicación (Persistencia)
            services.AddScoped<ICustomerNotificationChannelsUseCase, CustomerNotificationChannelsUseCase>();
            services.AddScoped<ICustomerNotificationEventsUseCase, CustomerNotificationEventsUseCase>();
            services.AddScoped<ICustomerNotificationsSettingsUseCase, CustomerNotificationsSettingsUseCase>();
            services.AddScoped<ICustomerUseCase, CustomerUseCase>();
            services.AddScoped<IMasterSettingsUseCase, MasterSettingsUseCase>();
            services.AddScoped<ICollaboratorUseCase, CollaboratorUseCase>();
            services.AddScoped<IOutboxMessagesUseCase, OutboxMessagesUseCase>();

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
