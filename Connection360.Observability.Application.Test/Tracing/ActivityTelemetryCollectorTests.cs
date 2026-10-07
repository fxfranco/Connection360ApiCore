using System.Diagnostics;
using Connection360.Observability.Application.Pipeline;
using Connection360.Observability.Application.Test.Support;
using Connection360.Observability.Application.Tracing;
using Connection360.Observability.Domain.Settings;
using Connection360.Observability.Domain.Telemetry;
using FluentAssertions;
using Xunit;

namespace Connection360.Observability.Application.Test.Tracing
{
    public class ActivityTelemetryCollectorTests : IAsyncLifetime
    {
        private readonly String _sourceName = "test.collector." + Guid.NewGuid();
        private readonly ActivitySource _source;
        private readonly TelemetryQueue<Activity> _queue = new("traces", 10_000);
        private ActivityTelemetryCollector? _collector;

        public ActivityTelemetryCollectorTests() => _source = new ActivitySource(_sourceName);

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            if (_collector is not null) await _collector.StopAsync(CancellationToken.None);
            _source.Dispose();
        }

        private async Task<ActivityTelemetryCollector> StartAsync(Action<TracesOptions>? configure = null)
        {
            var options = new ObservabilityOptions();
            options.Traces.IncludeSources = new List<String> { _sourceName };
            configure?.Invoke(options.Traces);
            _collector = new ActivityTelemetryCollector(_queue, options);
            await _collector.StartAsync(CancellationToken.None);
            return _collector;
        }

        [Fact]
        public async Task ActivityTerminado_SeEncolaParaSuEscritura()
        {
            await StartAsync();

            using (Activity? activity = _source.StartActivity("operacion"))
            {
                activity.Should().NotBeNull();
            }

            _queue.DrainAll().Should().ContainSingle().Which.OperationName.Should().Be("operacion");
        }

        [Fact]
        public async Task ActivityDeUnaFuenteNoIncluida_NoSeCaptura()
        {
            await StartAsync();
            using var other = new ActivitySource("test.otra." + Guid.NewGuid());

            Activity? activity = other.StartActivity("ajena");

            activity.Should().BeNull();
            _queue.DrainAll().Should().BeEmpty();
        }

        [Fact]
        public async Task ShouldListenTo_AceptaLaFuenteDeLaSolucionYLasIncluidas()
        {
            ActivityTelemetryCollector collector = await StartAsync();

            collector.ShouldListenTo(Connection360Telemetry.Source).Should().BeTrue();
            collector.ShouldListenTo(_source).Should().BeTrue();
            using var unknown = new ActivitySource("Otro.Origen");
            collector.ShouldListenTo(unknown).Should().BeFalse();
        }

        [Fact]
        public async Task ConSamplingEnCero_NoSeCapturaNingunaTraza()
        {
            await StartAsync(t => t.SamplingRatio = 0.0);

            for (Int32 i = 0; i < 50; i++)
            {
                using Activity? activity = _source.StartActivity("muestra");
                activity?.IsAllDataRequested.Should().BeFalse();
            }

            _queue.DrainAll().Should().BeEmpty();
        }

        [Fact]
        public async Task ConSamplingEnUno_SeCapturanTodas()
        {
            await StartAsync(t => t.SamplingRatio = 1.0);

            for (Int32 i = 0; i < 50; i++)
            {
                using Activity? activity = _source.StartActivity("muestra");
            }

            _queue.DrainAll().Should().HaveCount(50);
        }

        [Fact]
        public async Task ConSamplingParcial_SeCapturaUnaFraccionAproximada()
        {
            await StartAsync(t => t.SamplingRatio = 0.5);

            for (Int32 i = 0; i < 1000; i++)
            {
                using Activity? activity = _source.StartActivity("muestra");
            }

            Int32 captured = _queue.DrainAll().Count;
            captured.Should().BeInRange(380, 620);
        }

        [Fact]
        public async Task ConPadreMuestreado_SeRespetaLaDecisionDelPadreAunqueElRatioSeaCero()
        {
            await StartAsync(t => t.SamplingRatio = 0.0);
            var parent = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);

            using (Activity? activity = _source.StartActivity("hija", ActivityKind.Server, parent))
            {
                activity.Should().NotBeNull();
                activity!.IsAllDataRequested.Should().BeTrue();
            }

            _queue.DrainAll().Should().ContainSingle();
        }

        [Fact]
        public async Task ConPadreNoMuestreado_NoSeCapturaAunqueElRatioSeaUno()
        {
            await StartAsync(t => t.SamplingRatio = 1.0);
            var parent = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.None);

            using (Activity? activity = _source.StartActivity("hija", ActivityKind.Server, parent))
            {
                activity?.IsAllDataRequested.Should().BeFalse();
            }

            _queue.DrainAll().Should().BeEmpty();
        }

        [Fact]
        public async Task ConPadreRemotoPorTextoDeTraceparent_UsaElRatio()
        {
            await StartAsync(t => t.SamplingRatio = 1.0);

            using (Activity? activity = _source.StartActivity("hija", ActivityKind.Server, "00-0123456789abcdef0123456789abcdef-0123456789abcdef-01"))
            {
                activity.Should().NotBeNull();
            }

            _queue.DrainAll().Should().ContainSingle();
        }

        [Fact]
        public async Task PeticionesAUnaRutaExcluida_NoSeCapturan()
        {
            await StartAsync(t => t.ExcludePaths = new List<String> { "/health" });

            using (Activity? health = _source.StartActivity("GET /health", ActivityKind.Server))
            {
                health!.SetTag("url.path", "/health/ready");
            }

            using (Activity? api = _source.StartActivity("GET /api/clientes", ActivityKind.Server))
            {
                api!.SetTag("url.path", "/api/clientes");
            }

            _queue.DrainAll().Select(a => a.OperationName).Should().Equal("GET /api/clientes");
        }

        [Fact]
        public async Task LaExclusionDeRutas_SoloAplicaASpansDeTipoServidor()
        {
            await StartAsync(t => t.ExcludePaths = new List<String> { "/health" });

            using (Activity? client = _source.StartActivity("llamada saliente", ActivityKind.Client))
            {
                client!.SetTag("url.path", "/health");
            }

            _queue.DrainAll().Should().ContainSingle();
        }

        [Fact]
        public async Task DentroDeUnaSupresion_NoSeCreaElActivity()
        {
            await StartAsync();

            Activity? activity;
            using (TelemetrySuppression.Begin())
            {
                activity = _source.StartActivity("interno");
            }

            activity.Should().BeNull();
            _queue.DrainAll().Should().BeEmpty();
        }

        [Fact]
        public async Task DentroDeUnaSupresion_TampocoSeMuestreanPeticionesConPadreRemotoPorTexto()
        {
            await StartAsync();

            Activity? activity;
            using (TelemetrySuppression.Begin())
            {
                activity = _source.StartActivity("interno", ActivityKind.Server, "00-0123456789abcdef0123456789abcdef-0123456789abcdef-01");
            }

            activity.Should().BeNull();
        }

        [Fact]
        public async Task ConTrazasDeshabilitadas_NoSeSuscribe()
        {
            await StartAsync(t => t.Enabled = false);

            Activity? activity = _source.StartActivity("operacion");

            activity.Should().BeNull();
            _queue.DrainAll().Should().BeEmpty();
        }

        [Fact]
        public async Task StopAsync_DejaDeCapturar()
        {
            ActivityTelemetryCollector collector = await StartAsync();
            await collector.StopAsync(CancellationToken.None);

            Activity? activity = _source.StartActivity("despues");

            activity.Should().BeNull();
        }

        [Fact]
        public async Task StartAsync_LlamadoDosVeces_NoDuplicaLosRegistros()
        {
            ActivityTelemetryCollector collector = await StartAsync();
            await collector.StartAsync(CancellationToken.None);

            using (_source.StartActivity("una vez")) { }

            _queue.DrainAll().Should().ContainSingle();
        }
    }
}
