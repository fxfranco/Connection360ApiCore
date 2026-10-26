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
using System.Net.Http;

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
            services.Configure<EtlChangeTrackingSettings>(configuration.GetSection(EtlChangeTrackingSettings.SectionName));
            services.Configure<MaterializedViewSettings>(configuration.GetSection(MaterializedViewSettings.SectionName));

            // Cargar la sección directamente para poder registrar un HttpClient con nombre por cada API
            var externalApiSettings = configuration.GetSection(ExternalApiSettings.SectionName).Get<ExternalApiSettings>();

            if (externalApiSettings?.Apis != null)
            {
                foreach (var (apiName, config) in externalApiSettings.Apis)
                {
                    IHttpClientBuilder httpClientBuilder = services.AddHttpClient(apiName, client =>
                    {
                        client.BaseAddress = new Uri(config.BaseUrl);
                        client.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);

                        if (!String.IsNullOrWhiteSpace(config.ApiKey))
                        {
                            client.DefaultRequestHeaders.Add("X-Api-Key", config.ApiKey);
                        }
                    });

                    // SOLO DESARROLLO: únicamente cuando se activa explícitamente el interruptor se
                    // cambia el handler primario para tolerar certificados TLS de desarrollo (autofirmado,
                    // nombre que no coincide, cadena no confiable), pero NO vencidos, revocados ni inválidos
                    // (ver DevelopmentServerCertificateValidator). Con el valor por defecto (false) no se
                    // toca el handler y se aplica la validación estándar de .NET.
                    if (externalApiSettings.AllowInvalidServerCertificate)
                    {
                        httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                        {
                            ServerCertificateCustomValidationCallback = DevelopmentServerCertificateValidator.Validate
                        });
                    }
                }
            }

            services.AddScoped<IExternalDataGateway, ExternalDataApiGateway>();

            // Servicios de dominio (Transform)
            services.AddScoped<IDynamicDataSetMerger, DynamicDataSetMerger>();
            services.AddScoped<IShipmentsDataSheetMappingService, ShipmentsDataSheetMappingService>();

            // Transform del proceso ETL de logs (independiente del anterior).
            services.AddScoped<ILogStatusMappingService, LogStatusMappingService>();

            // Detección de cambios de ESTADO/COMENTARIO (dominio, sin I/O) y catálogo de mensajes de
            // notificación asociados - ver RunEtlProcessUseCase / EtlChangeNotifier.
            services.AddScoped<IApplicationDataSheetChangeDetector, ApplicationDataSheetChangeDetector>();
            services.AddScoped<IEtlChangeMessageCatalog, EtlChangeMessageCatalog>();

            // Persistencia (Load) - misma composición Session/UnitOfWork/Repositorio que Connection360.Infrastructure
            services.AddScoped<DbSession>();
            services.AddScoped<IApplicationDataSheetRepository, ApplicationDataSheetRepository>();
            services.AddScoped<ILogStatusTrackingRepository, LogStatusTrackingRepository>();
            services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<IMaterializedViewRepository, MaterializedViewRepository>();
            services.AddScoped<ICustomerNotificationChannelsRepository, CustomerNotificationChannelsRepository>();
            services.AddScoped<IEtlJobControlRepository, EtlJobControlRepository>();
            services.AddScoped<IUnitOfWork, Connection360.Etl.Infrastructure.Persistence.UnitOfWork>();

            return services;
        }
    }
}
