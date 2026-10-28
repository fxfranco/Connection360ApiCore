using Connection360.Observability.Domain.Ports;
using Connection360.Observability.Domain.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Connection360.Observability.Application.DependencyInjection
{
    /// <summary>
    /// Resultado de <c>AddConnection360Observability</c>: permite conectar almacenamientos
    /// (MongoDB hoy; Elasticsearch, OTLP, Loki... mañana) sin tocar la canalización.
    /// </summary>
    public interface IObservabilityBuilder
    {
        IServiceCollection Services { get; }

        IConfiguration Configuration { get; }

        ObservabilityOptions Options { get; }

        /// <summary>false cuando la observabilidad está desactivada por configuración: los adaptadores no deben registrar nada.</summary>
        Boolean Enabled { get; }
    }

    internal sealed class ObservabilityBuilder : IObservabilityBuilder
    {
        public ObservabilityBuilder(IServiceCollection services, IConfiguration configuration, ObservabilityOptions options)
        {
            Services = services;
            Configuration = configuration;
            Options = options;
        }

        public IServiceCollection Services { get; }

        public IConfiguration Configuration { get; }

        public ObservabilityOptions Options { get; }

        public Boolean Enabled => Options.Enabled;
    }

    public static class ObservabilityBuilderExtensions
    {
        /// <summary>
        /// Registra un almacenamiento propio. La clase puede implementar uno, dos o los tres
        /// contratos (<see cref="ILogStore"/>, <see cref="IMetricStore"/>, <see cref="ITraceStore"/>) y
        /// queda registrada para cada uno de ellos. Se pueden registrar varios: todos reciben los datos.
        /// </summary>
        public static IObservabilityBuilder AddStore<TStore>(this IObservabilityBuilder builder) where TStore : class
        {
            if (!builder.Enabled)
            {
                return builder;
            }

            builder.Services.TryAddSingleton<TStore>();
            if (typeof(ILogStore).IsAssignableFrom(typeof(TStore)))
            {
                builder.Services.AddSingleton<ILogStore>(sp => (ILogStore)sp.GetRequiredService<TStore>());
            }

            if (typeof(IMetricStore).IsAssignableFrom(typeof(TStore)))
            {
                builder.Services.AddSingleton<IMetricStore>(sp => (IMetricStore)sp.GetRequiredService<TStore>());
            }

            if (typeof(ITraceStore).IsAssignableFrom(typeof(TStore)))
            {
                builder.Services.AddSingleton<ITraceStore>(sp => (ITraceStore)sp.GetRequiredService<TStore>());
            }

            if (typeof(ITelemetryStoreInitializer).IsAssignableFrom(typeof(TStore)))
            {
                builder.Services.AddSingleton<ITelemetryStoreInitializer>(sp => (ITelemetryStoreInitializer)sp.GetRequiredService<TStore>());
            }

            return builder;
        }
    }
}
