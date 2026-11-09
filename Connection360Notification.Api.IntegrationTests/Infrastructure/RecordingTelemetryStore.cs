using Connection360.Observability.Domain.Models;
using Connection360.Observability.Domain.Ports;

namespace Connection360Notification.Api.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Almacenamiento de telemetría en memoria para las pruebas de integración: reemplaza a MongoDB
    /// (nunca hay conexiones reales) y permite comprobar qué logs, métricas y trazas produjo el host.
    /// </summary>
    public sealed class RecordingTelemetryStore : ILogStore, IMetricStore, ITraceStore, ITelemetryStoreInitializer
    {
        private readonly Object _gate = new();
        private readonly List<LogRecord> _logs = new();
        private readonly List<MetricRecord> _metrics = new();
        private readonly List<TraceRecord> _traces = new();

        public String Name => "recording";

        public Boolean Initialized { get; private set; }

        public IReadOnlyList<LogRecord> Logs { get { lock (_gate) return _logs.ToArray(); } }

        public IReadOnlyList<MetricRecord> Metrics { get { lock (_gate) return _metrics.ToArray(); } }

        public IReadOnlyList<TraceRecord> Traces { get { lock (_gate) return _traces.ToArray(); } }

        public Task WriteBatchAsync(IReadOnlyList<LogRecord> records, CancellationToken cancellationToken) { lock (_gate) _logs.AddRange(records); return Task.CompletedTask; }

        public Task WriteBatchAsync(IReadOnlyList<MetricRecord> records, CancellationToken cancellationToken) { lock (_gate) _metrics.AddRange(records); return Task.CompletedTask; }

        public Task WriteBatchAsync(IReadOnlyList<TraceRecord> records, CancellationToken cancellationToken) { lock (_gate) _traces.AddRange(records); return Task.CompletedTask; }

        public Task InitializeAsync(CancellationToken cancellationToken) { Initialized = true; return Task.CompletedTask; }

        /// <summary>Espera (sondeando) a que se cumpla la condición; falla con la descripción si vence el tiempo.</summary>
        public static async Task UntilAsync(Func<Boolean> condition, String description, Int32 timeoutMs = 15_000)
        {
            DateTime limit = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (!condition())
            {
                if (DateTime.UtcNow > limit) throw new TimeoutException("No se cumplió a tiempo: " + description);
                await Task.Delay(20);
            }
        }
    }
}
