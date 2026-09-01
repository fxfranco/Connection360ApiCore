using Connection360.Application.Ports;
using Connection360.Application.Ports.Output;
using Connection360.Domain.Interfaces;
using Connection360.Domain.Ports.Persistence;
using Connection360.Infrastructure.Adapters.Input;
using Connection360.Infrastructure.Adapters.Output;
using Connection360.Infrastructure.ExternalApi;
using Connection360.Infrastructure.Persistence;
using Connection360.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Connection360.Infrastructure.DependencyInjection
{
    /// <summary>
    /// Clase que carga las configuraciones que se necesitan en la capa de infraestructure
    /// </summary>
    public static class InfrastructureServiceCollectionExtensions
    {
        /// <summary>
        /// Carga toda la configuraciòn de la capa de infrastructure
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ExternalApiSettings>(configuration.GetSection(ExternalApiSettings.SectionName));

            // Cargar la sección directamente
            var externalApiSettings = configuration.GetSection(ExternalApiSettings.SectionName).Get<ExternalApiSettings>();

            if (externalApiSettings?.Apis != null)
            {
                // Recorrer cada API definida en el appsettings y registrar su HttpClient con nombre
                foreach (var (apiName, config) in externalApiSettings.Apis)
                {
                    services.AddHttpClient(apiName, client =>
                    {
                        client.BaseAddress = new Uri(config.BaseUrl);
                        client.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);

                        if (!String.IsNullOrWhiteSpace(config.ApiKey))
                        {
                            client.DefaultRequestHeaders.Add("X-Api-Key", config.ApiKey);
                        }
                    });
                }
            }
            services.AddScoped<IExternalDataGateway, ExternalDataApiGateway>();
            services.AddScoped<IExternalApiOpenStreetMap, ExternalApiOpenStreetMap>();


            services.AddSignalR();
            // Registrar el proveedor personalizado de ID de usuario para SignalR
            services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
            services.AddScoped<INotifierService, SignalRNotifierService>();

            // Auth0 UserManagements
            services.AddScoped<IAuth0UserService, Auth0UserService>();


            // 2. Sesión por Request (SCOPED) - Maneja el ciclo de vida de la IDbConnection y la Transacción activa
            services.AddScoped<DbSession>();

            // 3. Repositorios (SCOPED) - Piden la misma DbSession del request actual
            services.AddScoped<ICustomerNotificationChannelsRepository, CustomerNotificationChannelsRepository>();
            services.AddScoped<ICustomerNotificationEventRepository, CustomerNotificationEventRepository>();

            // 3. Registrar UnitOfWork como SCOPED (Garantiza 1 conexión/transacción por petición HTTP) (Persistencia)
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
