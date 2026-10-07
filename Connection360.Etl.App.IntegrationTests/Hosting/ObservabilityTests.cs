using Connection360.Etl.App.IntegrationTests.Infrastructure;
using Connection360.Observability.Domain.Models;
using Connection360.Observability.Domain.Ports;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Connection360.Etl.App.IntegrationTests.Hosting
{
    /// <summary>
    /// Ejecuta el Program.cs REAL del ETL con la observabilidad activa y un almacenamiento en memoria (sin MongoDB):
    /// verifica que el proceso (de corta vida, cuyo Host nunca se arranca) entrega al terminar los 3 pilares con la
    /// identidad "Etl", y que no se pierde nada del final de la ejecución.
    /// </summary>
    public class ObservabilityTests
    {
        private const String ValidConnection = "Host=db.connection360.test;Port=5432;Database=etl_test;Username=etl_user;Password=no-se-usa";

        private static ProgramRun RunProgram(ProgramFakes fakes, RecordingTelemetryStore store, params String[] extraArgs)
        {
            var args = new List<String> { $"--ConnectionStrings:PostgresConnection={ValidConnection}" };
            args.AddRange(extraArgs);
            return EtlProgramRunner.Run(services =>
            {
                fakes.Register(services);
                services.RemoveAll<ILogStore>();
                services.RemoveAll<IMetricStore>();
                services.RemoveAll<ITraceStore>();
                services.RemoveAll<ITelemetryStoreInitializer>();
                services.AddSingleton<ILogStore>(store);
                services.AddSingleton<IMetricStore>(store);
                services.AddSingleton<ITraceStore>(store);
                services.AddSingleton<ITelemetryStoreInitializer>(store);
            }, args.ToArray());
        }

        [Fact]
        public void Corrida_Exitosa_EntregaLosTresPilaresConLaIdentidadDelEtl()
        {
            var store = new RecordingTelemetryStore();

            var run = RunProgram(new ProgramFakes(), store);

            run.ExitCode.Should().Be(0);
            store.Logs.Should().NotBeEmpty().And.OnlyContain(l => l.Service == ObservedServices.Etl);
            store.Traces.Should().NotBeEmpty().And.OnlyContain(t => t.Service == ObservedServices.Etl);
            store.Metrics.Should().NotBeEmpty().And.OnlyContain(m => m.Service == ObservedServices.Etl);
            store.Initialized.Should().BeTrue();
        }

        [Fact]
        public void Corrida_Exitosa_GeneraUnaTrazaConElSpanRaizYUnSpanPorTarea()
        {
            var store = new RecordingTelemetryStore();

            RunProgram(new ProgramFakes(), store);

            TraceRecord root = store.Traces.Single(t => t.Name == "etl.process");
            TraceRecord logsJob = store.Traces.Single(t => t.Name == "etl.logs.run");
            TraceRecord mainJob = store.Traces.Single(t => t.Name == "etl.application_data_sheet.run");
            root.Status.Should().Be("Ok");
            root.ParentSpanId.Should().BeNull();
            root.Attributes["etl.success"].Should().Be(true);
            new[] { logsJob, mainJob }.Should().OnlyContain(job => job.TraceId == root.TraceId && job.ParentSpanId == root.SpanId, "las tareas son hijas del span raíz");
            logsJob.Status.Should().Be("Ok");
            logsJob.Attributes["etl.job"].Should().Be("logs");
            logsJob.Attributes["etl.records.loaded"].Should().Be(3L);
            mainJob.Attributes["etl.records.extracted"].Should().Be(7L);
            mainJob.Attributes["etl.records.loaded"].Should().Be(4L);
        }

        [Fact]
        public void Corrida_Exitosa_LosLogsQuedanCorrelacionadosConLaTraza()
        {
            var store = new RecordingTelemetryStore();

            RunProgram(new ProgramFakes(), store);

            TraceRecord root = store.Traces.Single(t => t.Name == "etl.process");
            LogRecord extract = store.Logs.Single(l => l.Message == "Extract [DATALOGS]: 3 registros");
            extract.TraceId.Should().Be(root.TraceId);
            extract.Category.Should().Be("Program");
            extract.MessageTemplate.Should().Be("Extract [{Api}]: {Count} registros");
            extract.Attributes["Api"].Should().Be("DATALOGS");
            extract.Attributes["Count"].Should().Be(3);
        }

        [Fact]
        public void Corrida_Exitosa_ReportaLasMetricasDelEtlDeLaUltimaEmision()
        {
            var store = new RecordingTelemetryStore();

            RunProgram(new ProgramFakes(), store);

            var runs = store.Metrics.Where(m => m.Name == "etl.runs").ToList();
            runs.Should().HaveCount(2);
            runs.Should().OnlyContain(m => (Boolean)m.Attributes["success"]! && m.Sum == 1 && m.Temporality == "Delta");
            runs.Select(m => (String)m.Attributes["etl.job"]!).Should().BeEquivalentTo("logs", "application_data_sheet");
            store.Metrics.Where(m => m.Name == "etl.records.loaded").Sum(m => m.Sum).Should().Be(7, "3 de logs + 4 del proceso principal");
            store.Metrics.Should().Contain(m => m.Name == "etl.run.duration" && m.Kind == "Histogram" && m.Unit == "s");
        }

        [Fact]
        public void Corrida_ConFallo_MarcaLosSpansEnErrorYSaleConUno()
        {
            var store = new RecordingTelemetryStore();
            var fakes = new ProgramFakes { MainResult = () => ProgramFakes.FailureResult("falló-principal") };

            var run = RunProgram(fakes, store);

            run.ExitCode.Should().Be(1);
            TraceRecord mainJob = store.Traces.Single(t => t.Name == "etl.application_data_sheet.run");
            mainJob.Status.Should().Be("Error");
            mainJob.StatusDescription.Should().Be("falló-principal");
            store.Traces.Single(t => t.Name == "etl.process").Status.Should().Be("Error");
            store.Traces.Single(t => t.Name == "etl.logs.run").Status.Should().Be("Ok");
            store.Logs.Should().Contain(l => l.Level == "Error" && l.Message == "Detalle del error: falló-principal");
            store.Metrics.Should().Contain(m => m.Name == "etl.runs" && (Boolean)m.Attributes["success"]! == false && (String)m.Attributes["etl.job"]! == "application_data_sheet");
        }

        [Fact]
        public void Corrida_ConExcepcionNoControlada_AunAsiVaciaLaTelemetriaPendiente()
        {
            var store = new RecordingTelemetryStore();
            var fakes = new ProgramFakes { MainResult = () => throw new InvalidOperationException("boom") };

            var run = RunProgram(fakes, store);

            run.Exception.Should().BeOfType<InvalidOperationException>();
            store.Traces.Should().Contain(t => t.Name == "etl.process", "el span raíz se cierra y se entrega aunque el proceso falle");
            store.Traces.Should().Contain(t => t.Name == "etl.logs.run");
            store.Logs.Should().Contain(l => l.Message.StartsWith("Corrida ETL (logs) finalizada"));
        }

        [Fact]
        public void Corrida_ConObservabilidadDeshabilitada_NoCapturaNadaYElEtlSigueIgual()
        {
            var store = new RecordingTelemetryStore();
            var fakes = new ProgramFakes();

            var run = RunProgram(fakes, store, "--Observability:Enabled=false");

            run.ExitCode.Should().Be(0);
            fakes.Probe.Calls.Should().Equal("logs", "main", "purge");
            store.Logs.Should().BeEmpty();
            store.Traces.Should().BeEmpty();
            store.Metrics.Should().BeEmpty();
            store.Initialized.Should().BeFalse();
        }

        [Fact]
        public void Corrida_ConTrazasDeshabilitadas_SiguePublicandoLogsYMetricas()
        {
            var store = new RecordingTelemetryStore();

            RunProgram(new ProgramFakes(), store, "--Observability:Traces:Enabled=false");

            store.Traces.Should().BeEmpty();
            store.Logs.Should().NotBeEmpty();
            store.Metrics.Should().NotBeEmpty();
        }

        [Fact]
        public void Corrida_DosVecesSeguidas_NoMezclaLaTelemetriaDeUnaConLaOtra()
        {
            var first = new RecordingTelemetryStore();
            var second = new RecordingTelemetryStore();

            RunProgram(new ProgramFakes(), first);
            RunProgram(new ProgramFakes(), second);

            first.Traces.Select(t => t.TraceId).Distinct().Should().ContainSingle();
            second.Traces.Select(t => t.TraceId).Distinct().Should().ContainSingle();
            first.Traces[0].TraceId.Should().NotBe(second.Traces[0].TraceId);
            first.Traces.Should().Contain(t => t.Name == "etl.process").Which.InstanceId.Should().Be(second.Traces.First(t => t.Name == "etl.process").InstanceId);
        }
    }
}
