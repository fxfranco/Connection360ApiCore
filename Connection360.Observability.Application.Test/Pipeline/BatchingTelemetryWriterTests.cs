using Connection360.Observability.Application.Pipeline;
using Connection360.Observability.Application.Test.Support;
using Connection360.Observability.Domain.Models;
using Connection360.Observability.Domain.Ports;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Connection360.Observability.Application.Test.Pipeline
{
    public class BatchingTelemetryWriterTests
    {
        private static LogRecord Log(String message) => new() { Message = message, Service = "Test", Timestamp = DateTime.UtcNow };

        private static BatchingTelemetryWriter<LogRecord, LogRecord> Writer(
            TelemetryQueue<LogRecord> queue, IEnumerable<ITelemetryStore<LogRecord>> stores, Int32 batchSize = 10, Int32 flushMs = 50,
            Func<LogRecord, LogRecord?>? map = null, Int32 shutdownSeconds = 5)
            => new(queue, map ?? (r => r), stores, batchSize, TimeSpan.FromMilliseconds(flushMs), TimeSpan.FromSeconds(shutdownSeconds), NullLogger.Instance);

        [Fact]
        public async Task ExecuteAsync_EscribeLosRegistrosEnLoteSegunElTamano()
        {
            var queue = new TelemetryQueue<LogRecord>("logs", 100);
            var store = new InMemoryStore<LogRecord>();
            using var writer = Writer(queue, new[] { store }, batchSize: 4, flushMs: 5_000);
            for (Int32 i = 0; i < 8; i++) queue.TryEnqueue(Log($"m{i}"));

            await writer.StartAsync(CancellationToken.None);
            await Wait.UntilAsync(() => store.Records.Count == 8, "8 registros escritos");
            await writer.StopAsync(CancellationToken.None);

            store.BatchSizes.Should().OnlyContain(size => size <= 4);
            store.Records.Select(r => r.Message).Should().Equal(Enumerable.Range(0, 8).Select(i => $"m{i}"));
        }

        [Fact]
        public async Task ExecuteAsync_ConUnLoteIncompleto_LoEscribePorTiempo()
        {
            var queue = new TelemetryQueue<LogRecord>("logs", 100);
            var store = new InMemoryStore<LogRecord>();
            using var writer = Writer(queue, new[] { store }, batchSize: 1000, flushMs: 50);
            await writer.StartAsync(CancellationToken.None);

            queue.TryEnqueue(Log("solo uno"));

            await Wait.UntilAsync(() => store.Records.Count == 1, "el lote incompleto se escribe por tiempo");
            await writer.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task StopAsync_VaciaLoQueQuedabaPendienteEnLaCola()
        {
            var queue = new TelemetryQueue<LogRecord>("logs", 1000);
            var store = new InMemoryStore<LogRecord>();
            using var writer = Writer(queue, new[] { store }, batchSize: 50, flushMs: 60_000);
            await writer.StartAsync(CancellationToken.None);
            for (Int32 i = 0; i < 120; i++) queue.TryEnqueue(Log($"m{i}"));

            await writer.StopAsync(CancellationToken.None);

            store.Records.Should().HaveCount(120);
        }

        [Fact]
        public async Task ExecuteAsync_ConVariosAlmacenamientos_TodosRecibenLosDatos()
        {
            var queue = new TelemetryQueue<LogRecord>("logs", 100);
            var a = new InMemoryStore<LogRecord>("a");
            var b = new InMemoryStore<LogRecord>("b");
            using var writer = Writer(queue, new[] { a, b });
            await writer.StartAsync(CancellationToken.None);
            queue.TryEnqueue(Log("x"));

            await writer.StopAsync(CancellationToken.None);

            a.Records.Should().HaveCount(1);
            b.Records.Should().HaveCount(1);
        }

        [Fact]
        public async Task ExecuteAsync_SiUnAlmacenamientoFalla_LosDemasSiguenRecibiendoYElEscritorNoSeCae()
        {
            var queue = new TelemetryQueue<LogRecord>("logs", 100);
            var broken = new InMemoryStore<LogRecord>("roto") { FailWith = new InvalidOperationException("caído") };
            var healthy = new InMemoryStore<LogRecord>("sano");
            using var writer = Writer(queue, new[] { broken, healthy }, batchSize: 1);
            await writer.StartAsync(CancellationToken.None);

            queue.TryEnqueue(Log("1"));
            await Wait.UntilAsync(() => healthy.Records.Count == 1, "el almacenamiento sano recibe el primer registro");
            queue.TryEnqueue(Log("2"));
            await Wait.UntilAsync(() => healthy.Records.Count == 2, "el sano sigue recibiendo después del fallo");
            await writer.StopAsync(CancellationToken.None);

            broken.Records.Should().BeEmpty();
            broken.Attempts.Should().BeGreaterThanOrEqualTo(2);
            writer.ExecuteTask?.IsFaulted.Should().NotBe(true);
        }

        [Fact]
        public async Task ExecuteAsync_ConUnFalloTransitorio_ReintentaUnaVezYEscribe()
        {
            var queue = new TelemetryQueue<LogRecord>("logs", 100);
            var flaky = new InMemoryStore<LogRecord>("intermitente") { FailFirst = 1 };
            using var writer = Writer(queue, new[] { flaky }, batchSize: 1);
            await writer.StartAsync(CancellationToken.None);

            queue.TryEnqueue(Log("reintento"));

            await Wait.UntilAsync(() => flaky.Records.Count == 1, "el reintento escribe el registro");
            flaky.Attempts.Should().Be(2);
            await writer.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task ExecuteAsync_SinAlmacenamientos_DescartaSinFallar()
        {
            var queue = new TelemetryQueue<LogRecord>("logs", 100);
            using var writer = Writer(queue, Array.Empty<ITelemetryStore<LogRecord>>());
            await writer.StartAsync(CancellationToken.None);
            queue.TryEnqueue(Log("a"));
            queue.TryEnqueue(Log("b"));

            Func<Task> stop = () => writer.StopAsync(CancellationToken.None);

            await stop.Should().NotThrowAsync();
        }

        [Fact]
        public async Task ExecuteAsync_ElMapeadorPuedeDescartarOFallarEnUnElementoSinAfectarAlResto()
        {
            var queue = new TelemetryQueue<LogRecord>("logs", 100);
            var store = new InMemoryStore<LogRecord>();
            using var writer = Writer(queue, new[] { store }, map: r => r.Message switch
            {
                "omitir" => null,
                "explota" => throw new InvalidOperationException("mapeo inválido"),
                _ => r,
            });
            await writer.StartAsync(CancellationToken.None);
            queue.TryEnqueue(Log("omitir"));
            queue.TryEnqueue(Log("explota"));
            queue.TryEnqueue(Log("bueno"));

            await writer.StopAsync(CancellationToken.None);

            store.Records.Select(r => r.Message).Should().Equal("bueno");
        }

        [Fact]
        public async Task ExecuteAsync_LasLlamadasAlAlmacenamientoSeEjecutanSuprimiendoLaTelemetria()
        {
            var queue = new TelemetryQueue<LogRecord>("logs", 10);
            Boolean? suppressed = null;
            var store = new CallbackStore(() => suppressed = TelemetrySuppression.IsSuppressed);
            using var writer = Writer(queue, new[] { store }, batchSize: 1);
            await writer.StartAsync(CancellationToken.None);

            queue.TryEnqueue(Log("x"));
            await Wait.UntilAsync(() => suppressed is not null, "el almacenamiento fue invocado");
            await writer.StopAsync(CancellationToken.None);

            suppressed.Should().BeTrue();
        }

        [Fact]
        public async Task StopAsync_SinHaberArrancado_NoLanza()
        {
            var queue = new TelemetryQueue<LogRecord>("logs", 10);
            using var writer = Writer(queue, new[] { new InMemoryStore<LogRecord>() });

            Func<Task> stop = () => writer.StopAsync(CancellationToken.None);

            await stop.Should().NotThrowAsync();
        }

        private sealed class CallbackStore : ITelemetryStore<LogRecord>
        {
            private readonly Action _callback;
            public CallbackStore(Action callback) => _callback = callback;
            public String Name => "callback";
            public Task WriteBatchAsync(IReadOnlyList<LogRecord> records, CancellationToken cancellationToken) { _callback(); return Task.CompletedTask; }
        }
    }
}
