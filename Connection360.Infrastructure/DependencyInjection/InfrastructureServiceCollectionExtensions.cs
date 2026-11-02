using Connection360.Application.Ports;
using Connection360.Domain.Interfaces;
using Connection360.Domain.Ports.Persistence;
using Connection360.Infrastructure.Adapters.Output;
using Connection360.Infrastructure.Caching;
using Connection360.Infrastructure.ExternalApi;
using Connection360.Infrastructure.Messaging;
using Connection360.Infrastructure.Persistence;
using Connection360.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

            // Intervalo de sondeo del OutboxPublisherWorker: se registra con IOptionsMonitor (vía
            // Configure<T>) para que el valor configurado en el appsettings de Connection360.Api
            // pueda cambiarse en caliente, sin recompilar ni reiniciar la aplicación.
            services.Configure<OutboxPublisherSettings>(configuration.GetSection(OutboxPublisherSettings.SectionName));

            // Periodicidad (y demás parámetros) del Auth0UserSyncWorker: también con IOptionsMonitor para
            // poder cambiarla en caliente desde el appsettings.
            services.Configure<Auth0UserSyncSettings>(configuration.GetSection(Auth0UserSyncSettings.SectionName));

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
            AddCoordinatesCache(services, configuration);

            // Auth0 UserManagements
            services.AddScoped<IAuth0UserService, Auth0UserService>();


            // 2. Sesión por Request (SCOPED) - Maneja el ciclo de vida de la IDbConnection y la Transacción activa
            services.AddScoped<DbSession>();

            // 3. Repositorios (SCOPED) - Piden la misma DbSession del request actual
            services.AddScoped<ICustomerNotificationChannelsRepository, CustomerNotificationChannelsRepository>();
            services.AddScoped<ICustomerNotificationEventRepository, CustomerNotificationEventRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<IMasterSettingsRepository, MasterSettingsRepository>();
            services.AddScoped<ICollaboratorRepository, CollaboratorRepository>();
            services.AddScoped<ICollaboratorNotificationChannelsRepository, CollaboratorNotificationChannelsRepository>();
            services.AddScoped<ICustomersOfCollaboratorsRepository, CustomersOfCollaboratorsRepository>();
            services.AddScoped<IOutboxMessagesRepository, OutboxMessagesRepository>();
            services.AddScoped<IApplicationDataSheetEntregadosRepository, ApplicationDataSheetEntregadosRepository>();
            services.AddScoped<IApplicationDataSheetNoEntregadosRepository, ApplicationDataSheetNoEntregadosRepository>();
            services.AddScoped<ILogStatusTrackingViewRepository, LogStatusTrackingViewRepository>();

            // 3. Registrar UnitOfWork como SCOPED (Garantiza 1 conexión/transacción por petición HTTP) (Persistencia)
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // 3. Worker Background Service para Consumo de Kafka
            services.AddHostedService<OutboxPublisherWorker>();

            // 4. Worker que sincroniza los usuarios de Auth0 hacia Connection360Notification.Api (vía Kafka)
            services.AddHostedService<Auth0UserSyncWorker>();

            return services;
        }

        /// <summary>
        /// Registra el adaptador de OpenStreetMap. Si la caché está habilitada (sección "CoordinatesCache",
        /// habilitada por defecto) lo envuelve con <see cref="CachedExternalApiOpenStreetMap"/>: el caso de uso
        /// sigue dependiendo solo del puerto <see cref="IExternalApiOpenStreetMap"/>. La caché es singleton
        /// (se comparte entre peticiones) y el adaptador real sigue siendo scoped.
        /// </summary>
        private static void AddCoordinatesCache(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ExternalApiOpenStreetMap>();

            IConfigurationSection cacheSection = configuration.GetSection(CoordinatesCacheSettings.SectionName);
            Boolean cacheEnabled = cacheSection.GetValue<Boolean?>(nameof(CoordinatesCacheSettings.Enabled)) ?? true;

            if (!cacheEnabled)
            {
                services.AddScoped<IExternalApiOpenStreetMap>(sp => sp.GetRequiredService<ExternalApiOpenStreetMap>());
                return;
            }

            services.AddSingleton<IValidateOptions<CoordinatesCacheSettings>, CoordinatesCacheSettingsValidator>();
            services.AddOptions<CoordinatesCacheSettings>().Bind(cacheSection).ValidateOnStart();
            services.AddSingleton<ICoordinatesCache, MemoryCoordinatesCache>();
            services.AddScoped<IExternalApiOpenStreetMap>(sp => new CachedExternalApiOpenStreetMap(
                sp.GetRequiredService<ExternalApiOpenStreetMap>(),
                sp.GetRequiredService<ICoordinatesCache>()));
        }
    }
}
