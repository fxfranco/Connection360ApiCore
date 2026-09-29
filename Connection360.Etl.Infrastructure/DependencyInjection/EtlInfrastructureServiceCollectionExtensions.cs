using Connection360.Etl.Application.Ports;
using Connection360.Etl.Domain.Interfaces;
using Connection360.Etl.Domain.Ports.Persistence;
using Connection360.Etl.Domain.Services;
using Connection360.Etl.Infrastructure.Configuration;
using Connection360.Etl.Infrastructure.ExternalApi;
using Connection360.Etl.Infrastructure.Persistence;
using Connection360.Etl.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Connection360.Etl.Infrastructure.DependencyInjection
{
    /// <summary>
    /// Registra todas las dependencias de infraestructura del proceso ETL. Copiado del mismo
    /// patrón que Connection360.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions
    /// (mismo bucle para registrar un <see cref="System.Net.Http.HttpClient"/> nombrado por cada API
    /// configurada en "ExternalApi").
    /// </summary>
    public static class EtlInfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddEtlInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ExternalApiSettings>(configuration.GetSection(ExternalApiSettings.SectionName));
            services.Configure<EtlJobControlSettings>(configuration.GetSection(EtlJobControlSettings.SectionName));

            // Cargar la sección directamente para poder registrar un HttpClient con nombre por cada API
            var externalApiSettings = configuration.GetSection(ExternalApiSettings.SectionName).Get<ExternalApiSettings>();

            if (externalApiSettings?.Apis != null)
            {
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

            // Servicios de dominio (Transform)
            services.AddScoped<IDynamicDataSetMerger, DynamicDataSetMerger>();
            services.AddScoped<IShipmentsDataSheetMappingService, ShipmentsDataSheetMappingService>();

            // Transform del proceso ETL de logs (independiente del anterior).
            services.AddScoped<ILogStatusMappingService, LogStatusMappingService>();

            // Persistencia (Load) - misma composición Session/UnitOfWork/Repositorio que Connection360.Infrastructure
            services.AddScoped<DbSession>();
            services.AddScoped<IApplicationDataSheetRepository, ApplicationDataSheetRepository>();
            services.AddScoped<ILogStatusTrackingRepository, LogStatusTrackingRepository>();
            services.AddScoped<IEtlJobControlRepository, EtlJobControlRepository>();
            services.AddScoped<IUnitOfWork, Connection360.Etl.Infrastructure.Persistence.UnitOfWork>();

            return services;
        }
    }
}
