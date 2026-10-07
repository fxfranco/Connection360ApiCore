using System.Diagnostics;
using System.Reflection;
using Connection360.Observability.Application.Initialization;
using Connection360.Observability.Application.Logging;
using Connection360.Observability.Application.Metrics;
using Connection360.Observability.Application.Pipeline;
using Connection360.Observability.Application.Tracing;
using Connection360.Observability.Domain.Models;
using Connection360.Observability.Domain.Ports;
using Connection360.Observability.Domain.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Connection360.Observability.Application.DependencyInjection
{
    public static class ObservabilityServiceCollectionExtensions
    {
        /// <summary>
        /// Activa los 3 pilares de observabilidad (logs, métricas y trazas) con las librerías nativas
        /// de .NET. Lee la sección "Observability" del appsettings. Después se conecta un
        /// almacenamiento con el builder devuelto (por ejemplo <c>.AddMongoObservabilityStores()</c>).
        /// </summary>
        /// <param name="serviceName">Una de <see cref="ObservedServices"/>: ApiCore, ApiNotification o Etl.</param>
        public static IObservabilityBuilder AddConnection360Observability(
            this IServiceCollection services,
            IConfiguration configuration,
            String serviceName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

            IConfigurationSection section = configuration.GetSection(ObservabilityOptions.SectionName);
            var options = new ObservabilityOptions();
            section.Bind(options);
            Normalize(options);

            services.AddSingleton(Options.Create(options));
            var builder = new ObservabilityBuilder(services, configuration, options);
            if (!options.Enabled)
            {
                return builder;
            }

            services.AddSingleton(sp => CreateIdentity(serviceName, sp.GetService<IHostEnvironment>(), configuration));

            // ---- Colas (una por señal) ----
            services.AddSingleton(new TelemetryQueue<LogRecord>("logs", options.Logs.QueueCapacity));
            services.AddSingleton(new TelemetryQueue<MetricRecord>("metrics", options.Metrics.QueueCapacity));
            services.AddSingleton(new TelemetryQueue<Activity>("traces", options.Traces.QueueCapacity));

            // ---- Componentes en segundo plano. El orden importa: al apagar, el Host los detiene en orden
            //      inverso, así los recolectores hacen su última emisión ANTES de que los escritores vacíen las colas. ----
            AddComponent(services, sp => new TelemetryStoreInitializationService(
                sp.GetServices<ITelemetryStoreInitializer>(), InternalLogger(sp, nameof(TelemetryStoreInitializationService))));

            if (options.Logs.Enabled)
            {
                AddComponent(services, sp => new BatchingTelemetryWriter<LogRecord, LogRecord>(
                    sp.GetRequiredService<TelemetryQueue<LogRecord>>(), record => record, sp.GetServices<ILogStore>(),
                    options.Logs.BatchSize, TimeSpan.FromSeconds(options.Logs.FlushIntervalSeconds),
                    TimeSpan.FromSeconds(options.ShutdownTimeoutSeconds), InternalLogger(sp, "LogsWriter")));
            }

            if (options.Metrics.Enabled)
            {
                AddComponent(services, sp => new BatchingTelemetryWriter<MetricRecord, MetricRecord>(
                    sp.GetRequiredService<TelemetryQueue<MetricRecord>>(), record => record, sp.GetServices<IMetricStore>(),
                    options.Metrics.BatchSize, TimeSpan.FromSeconds(options.Metrics.FlushIntervalSeconds),
                    TimeSpan.FromSeconds(options.ShutdownTimeoutSeconds), InternalLogger(sp, "MetricsWriter")));
                AddComponent(services, sp => new MetricsTelemetryCollector(
                    sp.GetRequiredService<TelemetryQueue<MetricRecord>>(), sp.GetRequiredService<ServiceIdentity>(), options));
            }

            if (options.Traces.Enabled)
            {
                AddComponent(services, sp =>
                {
                    var mapper = new ActivityRecordMapper(sp.GetRequiredService<ServiceIdentity>(), options.MaxFieldLength);
                    return new BatchingTelemetryWriter<Activity, TraceRecord>(
                        sp.GetRequiredService<TelemetryQueue<Activity>>(), mapper.Map, sp.GetServices<ITraceStore>(),
                        options.Traces.BatchSize, TimeSpan.FromSeconds(options.Traces.FlushIntervalSeconds),
                        TimeSpan.FromSeconds(options.ShutdownTimeoutSeconds), InternalLogger(sp, "TracesWriter"));
                });
                AddComponent(services, sp => new ActivityTelemetryCollector(sp.GetRequiredService<TelemetryQueue<Activity>>(), options));
            }

            // ---- Logs: proveedor de ILogger ----
            if (options.Logs.Enabled)
            {
                services.AddSingleton<ILoggerProvider>(sp => new TelemetryLoggerProvider(
                    sp.GetRequiredService<TelemetryQueue<LogRecord>>(), sp.GetRequiredService<ServiceIdentity>(), options));
            }

            return builder;
        }

        private static void AddComponent<TComponent>(IServiceCollection services, Func<IServiceProvider, TComponent> factory)
            where TComponent : class, IObservabilityComponent
        {
            // Una sola instancia, visible como IHostedService (para el Host) y como IObservabilityComponent
            // (para procesos de corta vida que los arrancan a mano, ver ObservabilityLifetime).
            services.AddSingleton(factory);
            services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<TComponent>());
            services.AddSingleton<IObservabilityComponent>(sp => sp.GetRequiredService<TComponent>());
        }

        private static ILogger InternalLogger(IServiceProvider sp, String name)
            => sp.GetRequiredService<ILoggerFactory>().CreateLogger($"Connection360.Observability.{name}");

        private static ServiceIdentity CreateIdentity(String serviceName, IHostEnvironment? environment, IConfiguration configuration)
        {
            Assembly? entry = Assembly.GetEntryAssembly();
            String version = entry?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? entry?.GetName().Version?.ToString()
                ?? "unknown";
            String environmentName = environment?.EnvironmentName
                ?? configuration["DOTNET_ENVIRONMENT"]
                ?? configuration["ASPNETCORE_ENVIRONMENT"]
                ?? "Production";

            return new ServiceIdentity(serviceName, version, environmentName, $"{System.Environment.MachineName}:{System.Environment.ProcessId}");
        }

        /// <summary>Corrige valores inválidos de configuración (cero, negativos) con los valores por defecto.</summary>
        private static void Normalize(ObservabilityOptions options)
        {
            if (options.ShutdownTimeoutSeconds <= 0) options.ShutdownTimeoutSeconds = 10;
            if (options.MaxFieldLength <= 0) options.MaxFieldLength = 4096;
            foreach (PipelineOptions pipeline in new PipelineOptions[] { options.Logs, options.Metrics, options.Traces })
            {
                if (pipeline.QueueCapacity <= 0) pipeline.QueueCapacity = 10_000;
                if (pipeline.BatchSize <= 0) pipeline.BatchSize = 200;
                if (pipeline.FlushIntervalSeconds <= 0) pipeline.FlushIntervalSeconds = 2;
            }

            if (options.Metrics.CollectionIntervalSeconds <= 0) options.Metrics.CollectionIntervalSeconds = 30;
            if (options.Metrics.MaxSeriesPerInstrument <= 0) options.Metrics.MaxSeriesPerInstrument = 500;
            options.Traces.SamplingRatio = Math.Clamp(options.Traces.SamplingRatio, 0.0, 1.0);
        }
    }
}
