using Connection360.Observability.Application.DependencyInjection;
using Connection360.Observability.Domain.Ports;
using Connection360.Observability.Infrastructure.Mongo.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Connection360.Observability.Infrastructure.Mongo.DependencyInjection
{
    public static class MongoObservabilityServiceCollectionExtensions
    {
        /// <summary>
        /// Conecta la observabilidad a MongoDB (colecciones notificationLogs, notificationMetrics y
        /// notificationTraces). Configuración en "Observability:Mongo". Si no hay cadena de conexión
        /// no se registra nada y la telemetría simplemente se descarta (la aplicación sigue igual).
        /// </summary>
        /// <param name="fallbackSection">
        /// Sección existente con "ConnectionString" y "DatabaseName" que se usa cuando "Observability:Mongo"
        /// no los define (por ejemplo "MongoDbSettings" en Notification.Api, para usar la misma BD).
        /// </param>
        public static IObservabilityBuilder AddMongoObservabilityStores(this IObservabilityBuilder builder, String? fallbackSection = null)
        {
            if (!builder.Enabled)
            {
                return builder;
            }

            MongoObservabilityOptions resolved = Resolve(builder.Configuration, fallbackSection);
            if (!resolved.IsConfigured)
            {
                return builder;
            }

            builder.Services.AddSingleton(Options.Create(resolved));
            builder.Services.AddSingleton<IMongoObservabilityContext, MongoObservabilityContext>();
            builder.Services.AddSingleton<ITelemetryStoreInitializer, MongoObservabilityInitializer>();
            builder.Services.AddSingleton<ILogStore, MongoLogStore>();
            builder.Services.AddSingleton<IMetricStore, MongoMetricStore>();
            builder.Services.AddSingleton<ITraceStore, MongoTraceStore>();
            return builder;
        }

        /// <summary>Lee "Observability:Mongo" y completa lo que falte desde la sección alternativa.</summary>
        public static MongoObservabilityOptions Resolve(IConfiguration configuration, String? fallbackSection)
        {
            var options = new MongoObservabilityOptions();
            configuration.GetSection(MongoObservabilityOptions.SectionName).Bind(options);

            if (!String.IsNullOrWhiteSpace(fallbackSection))
            {
                IConfigurationSection fallback = configuration.GetSection(fallbackSection);
                if (String.IsNullOrWhiteSpace(options.ConnectionString))
                {
                    options.ConnectionString = fallback["ConnectionString"] ?? String.Empty;
                }

                if (String.IsNullOrWhiteSpace(options.DatabaseName))
                {
                    options.DatabaseName = fallback["DatabaseName"] ?? String.Empty;
                }
            }

            return options;
        }
    }
}
