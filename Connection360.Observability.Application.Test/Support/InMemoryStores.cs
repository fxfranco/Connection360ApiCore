using Connection360.Observability.Domain.Models;
using Connection360.Observability.Domain.Ports;

namespace Connection360.Observability.Application.Test.Support
{
    /// <summary>Almacenamiento en memoria para pruebas: guarda todo lo que recibe y permite simular fallos.</summary>
    public sealed class InMemoryStore<TRecord> : ITelemetryStore<TRecord> where TRecord : TelemetryRecord
    {
        private readonly Object _gate = new();
        private readonly List<TRecord> _records = new();
        private readonly List<Int32> _batchSizes = new();

        public InMemoryStore(String name = "memory") => Name = name;

        public String Name { get; }

        /// <summary>Si no es null, cada escritura la lanza (simula un almacenamiento caído).</summary>
        public Exception? FailWith { get; set; }

        /// <summary>Cantidad de fallos que se lanzan antes de empezar a funcionar (para probar el reintento).</summary>
        public Int32 FailFirst { get; set; }

        public Int32 Attempts { get; private set; }

        public IReadOnlyList<TRecord> Records { get { lock (_gate) return _records.ToArray(); } }

        public IReadOnlyList<Int32> BatchSizes { get { lock (_gate) return _batchSizes.ToArray(); } }

        public Task WriteBatchAsync(IReadOnlyList<TRecord> records, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                Attempts++;
                if (FailWith is not null) throw FailWith;
                if (FailFirst > 0) { FailFirst--; throw new InvalidOperationException("fallo simulado"); }
                _records.AddRange(records);
                _batchSizes.Add(records.Count);
            }

            return Task.CompletedTask;
        }
    }

    public sealed class InMemoryLogStore : ILogStore
    {
        public InMemoryStore<LogRecord> Inner { get; } = new("memory-logs");
        public String Name => Inner.Name;
        public Task WriteBatchAsync(IReadOnlyList<LogRecord> records, CancellationToken cancellationToken) => Inner.WriteBatchAsync(records, cancellationToken);
    }

    public sealed class InMemoryMetricStore : IMetricStore
    {
        public InMemoryStore<MetricRecord> Inner { get; } = new("memory-metrics");
        public String Name => Inner.Name;
        public Task WriteBatchAsync(IReadOnlyList<MetricRecord> records, CancellationToken cancellationToken) => Inner.WriteBatchAsync(records, cancellationToken);
    }

    public sealed class InMemoryTraceStore : ITraceStore
    {
        public InMemoryStore<TraceRecord> Inner { get; } = new("memory-traces");
        public String Name => Inner.Name;
        public Task WriteBatchAsync(IReadOnlyList<TraceRecord> records, CancellationToken cancellationToken) => Inner.WriteBatchAsync(records, cancellationToken);
    }

    public static class Wait
    {
        /// <summary>Espera (sondeando) a que se cumpla la condición; falla con la descripción si vence el tiempo.</summary>
        public static async Task UntilAsync(Func<Boolean> condition, String description, Int32 timeoutMs = 10_000)
        {
            DateTime limit = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (!condition())
            {
                if (DateTime.UtcNow > limit)
                {
                    throw new TimeoutException("No se cumplió a tiempo: " + description);
                }

                await Task.Delay(10);
            }
        }
    }
}
