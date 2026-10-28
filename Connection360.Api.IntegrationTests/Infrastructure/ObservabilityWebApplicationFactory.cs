using Connection360.Observability.Application.Pipeline;
using Connection360.Observability.Domain.Ports;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Connection360.Api.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Arranca Program.cs real con la observabilidad ACTIVA y un almacenamiento en memoria (sin MongoDB real).
    /// Se retiran los servicios hospedados que tocan infraestructura (OutboxPublisherWorker, que usa PostgreSQL/Kafka) y
    /// se vuelven a registrar solo los componentes de observabilidad.
    /// </summary>
    public class ObservabilityWebApplicationFactory : CustomWebApplicationFactory
    {
        public RecordingTelemetryStore Store { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.UseSetting("Observability:Logs:FlushIntervalSeconds", "1");
            builder.UseSetting("Observability:Traces:FlushIntervalSeconds", "1");
            builder.UseSetting("Observability:Metrics:FlushIntervalSeconds", "1");

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ILogStore>();
                services.RemoveAll<IMetricStore>();
                services.RemoveAll<ITraceStore>();
                services.RemoveAll<ITelemetryStoreInitializer>();
                services.AddSingleton<ILogStore>(Store);
                services.AddSingleton<IMetricStore>(Store);
                services.AddSingleton<ITraceStore>(Store);
                services.AddSingleton<ITelemetryStoreInitializer>(Store);

                services.RemoveAll<IHostedService>();
                services.AddSingleton<IHostedService>(sp => new ObservabilityHostBridge(sp.GetServices<IObservabilityComponent>()));
            });
        }
    }

    /// <summary>Inicia los componentes de observabilidad en orden y los detiene en orden inverso (como lo haría el Host).</summary>
    public sealed class ObservabilityHostBridge : IHostedService
    {
        private readonly IReadOnlyList<IObservabilityComponent> _components;

        public ObservabilityHostBridge(IEnumerable<IObservabilityComponent> components) => _components = components.ToList();

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (IObservabilityComponent component in _components) await component.StartAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (IObservabilityComponent component in _components.Reverse()) await component.StopAsync(cancellationToken);
        }
    }

    /// <summary>Observabilidad desactivada por configuración.</summary>
    public class ObservabilityDisabledWebApplicationFactory : CustomWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("Observability:Enabled", "false");
        }
    }
}
