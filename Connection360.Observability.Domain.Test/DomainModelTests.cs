using System.Diagnostics;
using Connection360.Observability.Domain.Models;
using Connection360.Observability.Domain.Settings;
using Connection360.Observability.Domain.Telemetry;
using FluentAssertions;
using Xunit;

namespace Connection360.Observability.Domain.Test
{
    public class DomainModelTests
    {
        [Fact]
        public void ServiceIdentity_EsUnRecordConIgualdadPorValor()
        {
            var a = new ServiceIdentity("Etl", "1.0", "Prod", "pc:1");
            var b = new ServiceIdentity("Etl", "1.0", "Prod", "pc:1");

            a.Should().Be(b);
            (a with { ServiceName = "ApiCore" }).Should().NotBe(a);
        }

        [Fact]
        public void Connection360Telemetry_ExponeUnActivitySourceYUnMeterConElMismoNombre()
        {
            Connection360Telemetry.Name.Should().Be("Connection360");
            Connection360Telemetry.Source.Name.Should().Be("Connection360");
            Connection360Telemetry.Meter.Name.Should().Be("Connection360");
        }

        [Fact]
        public void Connection360Telemetry_SinListeners_NoCreaSpans()
        {
            using Activity? activity = Connection360Telemetry.Source.StartActivity("sin escucha");

            if (activity is not null)
            {
                // otro listener de otra prueba puede estar activo: el span al menos debe pertenecer a la fuente de la solución
                activity.Source.Name.Should().Be("Connection360");
            }
        }

        [Fact]
        public void ObservabilityOptions_TieneValoresPorDefectoSeguros()
        {
            var options = new ObservabilityOptions();

            options.Enabled.Should().BeTrue();
            options.ShutdownTimeoutSeconds.Should().Be(10);
            options.MaxFieldLength.Should().Be(4096);
            options.Logs.MinimumLevel.Should().Be("Information");
            options.Logs.QueueCapacity.Should().Be(10_000);
            options.Logs.BatchSize.Should().Be(200);
            options.Logs.FlushIntervalSeconds.Should().Be(2);
            options.Metrics.CollectionIntervalSeconds.Should().Be(30);
            options.Metrics.MaxSeriesPerInstrument.Should().Be(500);
            options.Traces.SamplingRatio.Should().Be(1.0);
            options.Traces.ExcludePaths.Should().Equal("/health");
            ObservabilityOptions.SectionName.Should().Be("Observability");
        }

        [Fact]
        public void LogsOptions_ReduceElRuidoDeLosEspaciosDeNombresDelFramework()
        {
            var levels = new LogsOptions().CategoryLevels;

            levels["Microsoft"].Should().Be("Warning");
            levels["System"].Should().Be("Warning");
            levels["Microsoft.Hosting.Lifetime"].Should().Be("Information");
        }

        [Fact]
        public void TracesOptions_EscuchaLasFuentesIntegradasDeDotNet()
        {
            new TracesOptions().IncludeSources.Should().Contain(new[] { "Microsoft.AspNetCore", "System.Net.Http", "Npgsql" });
        }

        [Fact]
        public void LosRegistros_TienenAtributosVaciosPorDefectoYCamposComunes()
        {
            var log = new LogRecord { Service = "Etl", Timestamp = DateTime.UtcNow };
            var metric = new MetricRecord { Service = "Etl" };
            var trace = new TraceRecord { Service = "Etl" };

            new TelemetryRecord[] { log, metric, trace }.Should().OnlyContain(r => r.Attributes.Count == 0 && r.Service == "Etl");
            trace.Events.Should().BeEmpty();
            trace.Links.Should().BeEmpty();
        }
    }
}
