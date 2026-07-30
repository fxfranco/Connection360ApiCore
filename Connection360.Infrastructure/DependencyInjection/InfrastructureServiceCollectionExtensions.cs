using Connection360.Application.Ports;
using Connection360.Infrastructure.ExternalApi;
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

            services.AddHttpClient<IExternalDataGateway, ExternalDataApiGateway>((sp, client) =>
            {
                var settings = configuration
                    .GetSection(ExternalApiSettings.SectionName)
                    .Get<ExternalApiSettings>()!;

                client.BaseAddress = new Uri(settings.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
            });

            return services;
        }
    }
}
