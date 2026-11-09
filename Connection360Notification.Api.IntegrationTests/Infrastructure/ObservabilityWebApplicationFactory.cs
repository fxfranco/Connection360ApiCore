using Connection360.Observability.Application.Pipeline;
using Connection360.Observability.Domain.Ports;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Connection360Notification.Api.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Arranca Program.cs real con la observabilidad ACTIVA: los componentes (recolectores y escritores)
    /// se vuelven a registrar como servicios hospedados (la factory base retira todos) y MongoDB se
    /// reemplaza por un almacenamiento en memoria, de modo que no hay conexiones reales.
    /// </summary>
    public class ObservabilityWebApplicationFactory : CustomWebApplicationFactory
    {
        public RecordingTelemetryStore Store { get; } = new();

        protected override IDictionary<String, String?> ExtraSettings => new Dictionary<String, String?>
        {
            ["Observability:Logs:FlushIntervalSeconds"] = "1",
            ["Observability:Traces:FlushIntervalSeconds"] = "1",
            ["Observability:Metrics:FlushIntervalSeconds"] = "1",
        };

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

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
        protected override IDictionary<String, String?> ExtraSettings => new Dictionary<String, String?>
        {
            ["Observability:Enabled"] = "false",
        };
    }
}
