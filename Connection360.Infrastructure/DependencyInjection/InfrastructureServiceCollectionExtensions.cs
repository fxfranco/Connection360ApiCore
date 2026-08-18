using Connection360.Application.Ports;
using Connection360.Application.Ports.Output;
using Connection360.Infrastructure.Adapters.Input;
using Connection360.Infrastructure.Adapters.Output;
using Connection360.Infrastructure.ExternalApi;
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

            return services;
        }
    }
}
