using Connection360.Etl.Application.Ports;
using Connection360.Etl.Application.UseCases;
using Connection360.Etl.Domain.Interfaces;
using Connection360.Etl.Domain.Ports.Persistence;
using Connection360.Etl.Infrastructure.Configuration;
using Connection360.Etl.Infrastructure.ExternalApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Connection360.Etl.App.Extensions
{
    /// <summary>
    /// Composition root de la capa de aplicación del proceso ETL. Copiado del mismo patrón que
    /// Connection360.Api/Extensions/ServiceCollectionExtensions.cs: el proyecto Application no
    /// tiene su propia extensión de DI, es el ejecutable (Api o, en este caso, Etl.App) quien
    /// decide cómo se registran sus casos de uso.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEtlApplicationServices(this IServiceCollection services)
        {
            // RunEtlProcessUseCase/RunLogsEtlProcessUseCase (Application) no pueden depender de
            // ExternalApiSettings/EtlChangeTrackingSettings (Infrastructure), así que es esta fábrica
            // -al igual que la de IPurgeEtlJobControlUseCase más abajo- quien resuelve esos valores de
            // configuración y los pasa como tipos planos (Int32?/String). El mismo PageSize se usa
            // para las 5 APIs operativas (vía ExternalDataApiGateway) y para el que se registra en
            // etl_job_control, garantizando que ambos siempre coincidan.
            services.AddScoped<IRunEtlProcessUseCase>(sp =>
            {
                var externalApiSettings = sp.GetRequiredService<IOptions<ExternalApiSettings>>().Value;
                var changeTrackingSettings = sp.GetRequiredService<IOptions<EtlChangeTrackingSettings>>().Value;
                var externalDataGateway = sp.GetRequiredService<IExternalDataGateway>();
                var merger = sp.GetRequiredService<IDynamicDataSetMerger>();
                var mappingService = sp.GetRequiredService<IShipmentsDataSheetMappingService>();
                var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
                var changeDetector = sp.GetRequiredService<IApplicationDataSheetChangeDetector>();
                var messageCatalog = sp.GetRequiredService<IEtlChangeMessageCatalog>();
                var logger = sp.GetRequiredService<ILogger<RunEtlProcessUseCase>>();
                return new RunEtlProcessUseCase(
                    externalDataGateway,
                    merger,
                    mappingService,
                    unitOfWork,
                    ResolveGlobalPageSize(externalApiSettings),
                    changeDetector,
                    messageCatalog,
                    changeTrackingSettings.SystemUser,
                    logger);
            });

            // Proceso ETL de logs: independiente del anterior (su propia extracción, mapeo y repositorio),
            // pero comparte el mismo PageSize global para que su paginación sea consistente con la del
            // proceso principal.
            services.AddScoped<IRunLogsEtlProcessUseCase>(sp =>
            {
                var externalApiSettings = sp.GetRequiredService<IOptions<ExternalApiSettings>>().Value;
                var externalDataGateway = sp.GetRequiredService<IExternalDataGateway>();
                var mappingService = sp.GetRequiredService<ILogStatusMappingService>();
                var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
                var logger = sp.GetRequiredService<ILogger<RunLogsEtlProcessUseCase>>();
                return new RunLogsEtlProcessUseCase(
                    externalDataGateway,
                    mappingService,
                    unitOfWork,
                    ResolveGlobalPageSize(externalApiSettings),
                    logger);
            });

            // Depuración de etl_job_control: PurgeEtlJobControlUseCase (Application) no puede depender
            // de EtlJobControlSettings (Infrastructure), así que es esta fábrica -parte de la
            // composición de dependencias, la única capa que puede conocer ambos- la que resuelve el
            // valor de configuración y lo pasa como un Int32 plano al construir el caso de uso.
            services.AddScoped<IPurgeEtlJobControlUseCase>(sp =>
            {
                var retentionDays = sp.GetRequiredService<IOptions<EtlJobControlSettings>>().Value.ApplicationDataSheetRetentionDays;
                var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
                var logger = sp.GetRequiredService<ILogger<PurgeEtlJobControlUseCase>>();
                return new PurgeEtlJobControlUseCase(unitOfWork, retentionDays, logger);
            });

            return services;
        }

        /// <summary>
        /// Único punto donde se decide el PageSize efectivo: global para las 5 APIs operativas
        /// (vía ExternalDataApiGateway) y para el registro en etl_job_control. Si la paginación está
        /// deshabilitada o el PageSize configurado no es válido (&lt;= 0), se devuelve null, que es lo
        /// que RunEtlProcessUseCase/RunLogsEtlProcessUseCase interpretan como "sin paginación".
        /// </summary>
        private static Int32? ResolveGlobalPageSize(ExternalApiSettings settings) =>
            settings.PaginationEnabled && settings.PageSize > 0 ? settings.PageSize : null;
    }
}
