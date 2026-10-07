using System.Diagnostics.Metrics;
using Connection360.Observability.Application.Metrics;
using Connection360.Observability.Application.Pipeline;
using Connection360.Observability.Application.Test.Support;
using Connection360.Observability.Domain.Models;
using Connection360.Observability.Domain.Settings;
using FluentAssertions;
using Xunit;

namespace Connection360.Observability.Application.Test.Metrics
{
    public class MetricsTelemetryCollectorTests : IAsyncLifetime
    {
        private readonly String _meterName = "test.meter." + Guid.NewGuid();
        private readonly Meter _meter;
        private readonly TelemetryQueue<MetricRecord> _queue = new("metrics", 10_000);
        private MetricsTelemetryCollector? _collector;

        public MetricsTelemetryCollectorTests() => _meter = new Meter(_meterName, "3.1");

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            if (_collector is not null) await _collector.StopAsync(CancellationToken.None);
            _meter.Dispose();
        }

        private async Task<MetricsTelemetryCollector> StartAsync(Action<MetricsOptions>? configure = null)
        {
            var options = new ObservabilityOptions();
            options.Metrics.CollectionIntervalSeconds = 3600; // la emisión se fuerza con Collect()
            options.Metrics.IncludeMeters = new List<String> { _meterName };
            configure?.Invoke(options.Metrics);
            _collector = new MetricsTelemetryCollector(_queue, new ServiceIdentity("ApiCore", "1.0", "Testing", "pc:1"), options);
            await _collector.StartAsync(CancellationToken.None);
            return _collector;
        }

        [Fact]
        public async Task Counter_AcumulaLasMedicionesDelIntervalo()
        {
            MetricsTelemetryCollector collector = await StartAsync();
            Counter<Int64> counter = _meter.CreateCounter<Int64>("pruebas.contador", unit: "{item}", description: "Contador de prueba");

            counter.Add(2);
            counter.Add(3);
            collector.Collect();

            MetricRecord record = _queue.DrainAll().Should().ContainSingle().Subject;
            record.Name.Should().Be("pruebas.contador");
            record.Unit.Should().Be("{item}");
            record.Description.Should().Be("Contador de prueba");
            record.MeterName.Should().Be(_meterName);
            record.MeterVersion.Should().Be("3.1");
            record.Kind.Should().Be("Counter");
            record.Temporality.Should().Be("Delta");
            record.Count.Should().Be(2);
            record.Sum.Should().Be(5);
            record.Min.Should().Be(2);
            record.Max.Should().Be(3);
        }

        [Fact]
        public async Task Counter_IncluyeLaIdentidadDelServicio()
        {
            MetricsTelemetryCollector collector = await StartAsync();
            _meter.CreateCounter<Int32>("pruebas.id").Add(1);

            collector.Collect();

            MetricRecord record = _queue.DrainAll().Single();
            record.Service.Should().Be("ApiCore");
            record.ServiceVersion.Should().Be("1.0");
            record.Environment.Should().Be("Testing");
            record.InstanceId.Should().Be("pc:1");
            record.IntervalStart.Should().BeOnOrBefore(record.Timestamp);
        }

        [Fact]
        public async Task Counter_DespuesDeEmitir_ReiniciaElIntervalo()
        {
            MetricsTelemetryCollector collector = await StartAsync();
            Counter<Int32> counter = _meter.CreateCounter<Int32>("pruebas.reinicio");
            counter.Add(4);
            collector.Collect();
            _queue.DrainAll();

            collector.Collect();
            _queue.DrainAll().Should().BeEmpty("sin mediciones nuevas no se emite nada");

            counter.Add(1);
            collector.Collect();
            _queue.DrainAll().Single().Sum.Should().Be(1);
        }

        [Fact]
        public async Task Counter_SeparaUnaSeriePorCombinacionDeEtiquetas()
        {
            MetricsTelemetryCollector collector = await StartAsync();
            Counter<Int32> counter = _meter.CreateCounter<Int32>("pruebas.etiquetas");

            counter.Add(1, new KeyValuePair<String, Object?>("estado", "ok"));
            counter.Add(1, new KeyValuePair<String, Object?>("estado", "ok"));
            counter.Add(5, new KeyValuePair<String, Object?>("estado", "error"));
            collector.Collect();

            List<MetricRecord> records = _queue.DrainAll();
            records.Should().HaveCount(2);
            records.Single(r => (String)r.Attributes["estado"]! == "ok").Sum.Should().Be(2);
            records.Single(r => (String)r.Attributes["estado"]! == "error").Sum.Should().Be(5);
        }

        [Fact]
        public async Task Histogram_ReportaConteoSumaMinimoYMaximo()
        {
            MetricsTelemetryCollector collector = await StartAsync();
            Histogram<Double> histogram = _meter.CreateHistogram<Double>("pruebas.duracion", unit: "ms");

            histogram.Record(10);
            histogram.Record(30);
            histogram.Record(20);
            collector.Collect();

            MetricRecord record = _queue.DrainAll().Single();
            record.Kind.Should().Be("Histogram");
            record.Count.Should().Be(3);
            record.Sum.Should().Be(60);
            record.Min.Should().Be(10);
            record.Max.Should().Be(30);
        }

        [Fact]
        public async Task UpDownCounter_LlevaElTotalCorrienteComoUltimoValor()
        {
            MetricsTelemetryCollector collector = await StartAsync();
            UpDownCounter<Int32> counter = _meter.CreateUpDownCounter<Int32>("pruebas.activos");

            counter.Add(5);
            counter.Add(-2);
            collector.Collect();
            _queue.DrainAll().Single().Last.Should().Be(3);

            counter.Add(1);
            collector.Collect();
            MetricRecord second = _queue.DrainAll().Single();
            second.Last.Should().Be(4, "el total corriente se conserva entre intervalos");
            second.Sum.Should().Be(1);
        }

        [Fact]
        public async Task ObservableGauge_SeLeeEnCadaEmision()
        {
            MetricsTelemetryCollector collector = await StartAsync();
            Int32 value = 7;
            _meter.CreateObservableGauge("pruebas.gauge", () => value);

            collector.Collect();
            MetricRecord first = _queue.DrainAll().Single();
            value = 9;
            collector.Collect();
            MetricRecord second = _queue.DrainAll().Single();

            first.Kind.Should().Be("ObservableGauge");
            first.Temporality.Should().Be("Cumulative");
            first.Last.Should().Be(7);
            second.Last.Should().Be(9);
        }

        [Fact]
        public async Task ObservableCounter_ReportaElValorAcumuladoActual()
        {
            MetricsTelemetryCollector collector = await StartAsync();
            _meter.CreateObservableCounter("pruebas.observable", () => 123L);

            collector.Collect();

            MetricRecord record = _queue.DrainAll().Single();
            record.Kind.Should().Be("ObservableCounter");
            record.Last.Should().Be(123);
        }

        [Fact]
        public async Task ObservableDefectuoso_NoImpideRecolectarLosDemas()
        {
            MetricsTelemetryCollector collector = await StartAsync();
            Func<Int32> broken = () => throw new InvalidOperationException("falla del callback");
            _meter.CreateObservableGauge("pruebas.roto", broken);
            _meter.CreateCounter<Int32>("pruebas.sano").Add(1);

            Action act = () => collector.Collect();

            act.Should().NotThrow();
            _queue.DrainAll().Select(r => r.Name).Should().Contain("pruebas.sano");
        }

        [Fact]
        public async Task SobrePasarElMaximoDeSeries_AgrupaElExcedenteEnUnaSerieDeDesbordamiento()
        {
            MetricsTelemetryCollector collector = await StartAsync(m => m.MaxSeriesPerInstrument = 2);
            Counter<Int32> counter = _meter.CreateCounter<Int32>("pruebas.cardinalidad");

            for (Int32 i = 0; i < 10; i++)
            {
                counter.Add(1, new KeyValuePair<String, Object?>("usuario", $"u{i}"));
            }

            collector.Collect();

            List<MetricRecord> records = _queue.DrainAll();
            records.Should().HaveCount(3);
            MetricRecord overflow = records.Single(r => r.Attributes.ContainsKey("otel.metric.overflow"));
            overflow.Sum.Should().Be(8);
        }

        [Fact]
        public async Task MetersExcluidos_NoSeRecolectan()
        {
            MetricsTelemetryCollector collector = await StartAsync(m => m.ExcludeMeters = new List<String> { _meterName });
            _meter.CreateCounter<Int32>("pruebas.excluido").Add(1);

            collector.Collect();

            _queue.DrainAll().Should().BeEmpty();
        }

        [Fact]
        public async Task SinFiltroDeInclusion_RecolectaCualquierMeter()
        {
            MetricsTelemetryCollector collector = await StartAsync(m => m.IncludeMeters = new List<String>());

            collector.IsIncluded("cualquier.meter").Should().BeTrue();
        }

        [Fact]
        public async Task IsIncluded_RespetaPrefijosYExclusionTienePrioridad()
        {
            MetricsTelemetryCollector collector = await StartAsync(m =>
            {
                m.IncludeMeters = new List<String> { "Microsoft.AspNetCore", "Connection360" };
                m.ExcludeMeters = new List<String> { "Microsoft.AspNetCore.Internal" };
            });

            collector.IsIncluded("Microsoft.AspNetCore.Hosting").Should().BeTrue();
            collector.IsIncluded("Microsoft.AspNetCore.Internal.Algo").Should().BeFalse();
            collector.IsIncluded("System.Net.Http").Should().BeFalse();
        }

        [Fact]
        public async Task ConMetricasDeshabilitadas_NoRecolecta()
        {
            MetricsTelemetryCollector collector = await StartAsync(m => m.Enabled = false);
            _meter.CreateCounter<Int32>("pruebas.apagado").Add(1);

            collector.Collect();

            _queue.DrainAll().Should().BeEmpty();
        }

        [Fact]
        public async Task StopAsync_HaceUnaUltimaEmisionConLoPendiente()
        {
            MetricsTelemetryCollector collector = await StartAsync();
            _meter.CreateCounter<Int32>("pruebas.final").Add(9);

            await collector.StopAsync(CancellationToken.None);

            _queue.DrainAll().Should().ContainSingle().Which.Sum.Should().Be(9);
        }

        [Fact]
        public async Task StopAsync_SinHaberIniciado_NoLanza()
        {
            var collector = new MetricsTelemetryCollector(_queue, new ServiceIdentity("a", "1", "t", "i"), new ObservabilityOptions());

            Func<Task> act = () => collector.StopAsync(CancellationToken.None);

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task ElCicloPeriodico_EmiteSinNecesidadDeLlamarACollect()
        {
            MetricsTelemetryCollector collector = await StartAsync(m => m.CollectionIntervalSeconds = 1);
            _meter.CreateCounter<Int32>("pruebas.periodico").Add(1);

            await Wait.UntilAsync(() => _queue.Reader.Count > 0, "el temporizador emite la métrica", timeoutMs: 5_000);
        }

        [Fact]
        public async Task MedicionesConcurrentes_NoPierdenDatos()
        {
            MetricsTelemetryCollector collector = await StartAsync();
            Counter<Int32> counter = _meter.CreateCounter<Int32>("pruebas.concurrente");

            await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
            {
                for (Int32 i = 0; i < 5_000; i++) counter.Add(1);
            })));
            collector.Collect();

            MetricRecord record = _queue.DrainAll().Single();
            record.Count.Should().Be(40_000);
            record.Sum.Should().Be(40_000);
        }
    }
}
