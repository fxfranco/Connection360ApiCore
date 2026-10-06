using Connection360.Etl.Application.DTOs;
using Connection360.Etl.Application.Tests.Support;
using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Domain.Enums;
using Connection360.Etl.Domain.Interfaces;
using Connection360.Etl.Domain.Ports.Persistence;
using Connection360.Etl.Application.Ports;
using Connection360.Etl.Application.UseCases;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Connection360.Etl.Application.Tests.UseCases
{
    public class RunLogsEtlProcessUseCaseTests
    {
        private const Int64 ControlId = 300;

        private sealed class Harness
        {
            public List<String> Calls { get; } = new();
            public List<(String Api, IDictionary<String, String> Filters)> FetchRequests { get; } = new();
            public List<DynamicDataSet> Pages { get; } = new();
            public Mock<IExternalDataGateway> Gateway { get; } = new();
            public Mock<ILogStatusMappingService> Mapping { get; } = new();
            public Mock<IUnitOfWork> UnitOfWork { get; } = new();
            public Mock<IEtlJobControlRepository> JobControl { get; } = new();
            public Mock<ILogStatusTrackingRepository> LogRepository { get; } = new();
            public ListLogger<RunLogsEtlProcessUseCase> Logger { get; } = new();

            public Harness(Boolean alreadyCompleted = false)
            {
                UnitOfWork.Setup(u => u.GetRepository<IEtlJobControlRepository>()).Returns(JobControl.Object);
                UnitOfWork.Setup(u => u.GetRepository<ILogStatusTrackingRepository>()).Returns(LogRepository.Object);
                UnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Callback(() => Calls.Add("Begin")).Returns(Task.CompletedTask);
                UnitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Callback(() => Calls.Add("Commit")).Returns(Task.CompletedTask);
                UnitOfWork.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Callback(() => Calls.Add("Rollback")).Returns(Task.CompletedTask);

                JobControl.Setup(r => r.HasCompletedRunAsync(EtlJobName.LogStatusTracking, It.IsAny<CancellationToken>())).ReturnsAsync(alreadyCompleted);
                JobControl.Setup(r => r.StartRunAsync(EtlJobName.LogStatusTracking, It.IsAny<Int32?>(), It.IsAny<CancellationToken>()))
                    .Callback((EtlJobName _, Int32? size, CancellationToken _) => Calls.Add($"Start:{size?.ToString() ?? "null"}"))
                    .ReturnsAsync(ControlId);
                JobControl.Setup(r => r.RegisterPageProgressAsync(It.IsAny<Int64>(), It.IsAny<Int32>(), It.IsAny<Int32>(), It.IsAny<CancellationToken>()))
                    .Callback((Int64 id, Int32 page, Int32 count, CancellationToken _) => Calls.Add($"Progress:{id}:{page}:{count}"))
                    .Returns(Task.CompletedTask);
                JobControl.Setup(r => r.CompleteRunAsync(It.IsAny<Int64>(), It.IsAny<CancellationToken>()))
                    .Callback((Int64 id, CancellationToken _) => Calls.Add($"Complete:{id}")).Returns(Task.CompletedTask);
                JobControl.Setup(r => r.FailRunAsync(It.IsAny<Int64>(), It.IsAny<CancellationToken>()))
                    .Callback((Int64 id, CancellationToken _) => Calls.Add($"Fail:{id}")).Returns(Task.CompletedTask);

                Gateway.Setup(g => g.FetchDataPagedAsync(It.IsAny<String>(), It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>()))
                    .Returns((String api, IDictionary<String, String> filters, CancellationToken _) =>
                    {
                        FetchRequests.Add((api, filters));
                        Calls.Add("Fetch");
                        return AsyncSequence.From(Pages.ToArray());
                    });

                Mapping.Setup(m => m.Map(It.IsAny<DynamicDataSet>()))
                    .Returns((DynamicDataSet set) => set.Rows
                        .Select(r => new LogStatusTracking { DocumentoTransporteHbl = r["DOC"] })
                        .ToList());

                LogRepository.Setup(r => r.InsertBatchAsync(It.IsAny<IEnumerable<LogStatusTracking>>(), It.IsAny<CancellationToken>()))
                    .Callback(() => Calls.Add("Insert"))
                    .ReturnsAsync((IEnumerable<LogStatusTracking> rows, CancellationToken _) => rows.Count());
            }

            public static DynamicDataSet Page(params String[] docs)
                => new(new[] { "DOC" }, docs.Select(d => new DynamicRecord(new Dictionary<String, String> { ["DOC"] = d })));

            public RunLogsEtlProcessUseCase Create(Int32? pageSize = null)
                => new(Gateway.Object, Mapping.Object, UnitOfWork.Object, pageSize, Logger);
        }

        [Fact]
        public async Task ExecuteAsync_SinCorridaPreviaYUnaPagina_CargaTodoYMarcaCompleted()
        {
            var h = new Harness();
            h.Pages.Add(Harness.Page("L1", "L2", "L3"));

            EtlRunResult result = await h.Create().ExecuteAsync();

            result.Success.Should().BeTrue();
            result.ErrorMessage.Should().BeNull();
            result.TransformedRecords.Should().Be(3);
            result.LoadedRecords.Should().Be(3);
            result.ExtractedRecordsByApi.Should().ContainKey("DATALOGS").WhoseValue.Should().Be(3);
            result.IsInitialMigrationRun.Should().BeFalse();
            result.FinishedAtUtc.Should().BeOnOrAfter(result.StartedAtUtc);
            h.Calls.Should().Equal("Start:null", "Fetch", "Begin", "Insert", $"Progress:{ControlId}:1:3", "Commit", $"Complete:{ControlId}");
        }

        [Fact]
        public async Task ExecuteAsync_ConPageSize_LoGuardaEnElRegistroDeControl()
        {
            var h = new Harness();
            h.Pages.Add(Harness.Page("L1"));

            await h.Create(pageSize: 250).ExecuteAsync();

            h.JobControl.Verify(r => r.StartRunAsync(EtlJobName.LogStatusTracking, 250, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ConsultaUnicamenteDatalogsSinFiltros()
        {
            var h = new Harness();

            await h.Create().ExecuteAsync();

            h.FetchRequests.Should().ContainSingle();
            h.FetchRequests[0].Api.Should().Be("DATALOGS");
            h.FetchRequests[0].Filters.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteAsync_VariasPaginas_CadaPaginaVaEnSuPropiaTransaccionYAcumulaTotales()
        {
            var h = new Harness();
            h.Pages.Add(Harness.Page("L1", "L2"));
            h.Pages.Add(Harness.Page("L3"));
            h.Pages.Add(Harness.Page("L4", "L5", "L6"));

            EtlRunResult result = await h.Create(pageSize: 3).ExecuteAsync();

            result.Success.Should().BeTrue();
            result.LoadedRecords.Should().Be(6);
            result.TransformedRecords.Should().Be(6);
            result.ExtractedRecordsByApi["DATALOGS"].Should().Be(6);
            h.Calls.Count(c => c == "Begin").Should().Be(3);
            h.Calls.Count(c => c == "Commit").Should().Be(3);
            h.Calls.Should().ContainInOrder(
                $"Progress:{ControlId}:1:2", "Commit",
                $"Progress:{ControlId}:2:1", "Commit",
                $"Progress:{ControlId}:3:3", "Commit",
                $"Complete:{ControlId}");
        }

        [Fact]
        public async Task ExecuteAsync_SinPaginas_CompletaElJobSinCargarNada()
        {
            var h = new Harness();

            EtlRunResult result = await h.Create().ExecuteAsync();

            result.Success.Should().BeTrue();
            result.LoadedRecords.Should().Be(0);
            result.ExtractedRecordsByApi.Should().BeEmpty();
            h.Calls.Should().NotContain("Begin");
            h.JobControl.Verify(r => r.CompleteRunAsync(ControlId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_PaginaSinFilas_SeProcesaDeTodasFormas()
        {
            var h = new Harness();
            h.Pages.Add(Harness.Page());

            EtlRunResult result = await h.Create().ExecuteAsync();

            result.Success.Should().BeTrue();
            result.ExtractedRecordsByApi["DATALOGS"].Should().Be(0);
            h.LogRepository.Verify(r => r.InsertBatchAsync(It.IsAny<IEnumerable<LogStatusTracking>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_YaExisteCorridaCompleted_NoVuelveAEjecutarse()
        {
            var h = new Harness(alreadyCompleted: true);
            h.Pages.Add(Harness.Page("L1"));

            EtlRunResult result = await h.Create().ExecuteAsync();

            result.Success.Should().BeTrue();
            result.LoadedRecords.Should().Be(0);
            result.FinishedAtUtc.Should().BeOnOrAfter(result.StartedAtUtc);
            h.Gateway.Verify(g => g.FetchDataPagedAsync(It.IsAny<String>(), It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>()), Times.Never);
            h.JobControl.Verify(r => r.StartRunAsync(It.IsAny<EtlJobName>(), It.IsAny<Int32?>(), It.IsAny<CancellationToken>()), Times.Never);
            h.UnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
            h.Logger.Messages(LogLevel.Information).Should().Contain(m => m.Contains("ya se ejecutó exitosamente"));
        }

        [Fact]
        public async Task ExecuteAsync_FallaLaCarga_RevierteYMarcaFailed()
        {
            var h = new Harness();
            h.Pages.Add(Harness.Page("L1"));
            h.LogRepository.Setup(r => r.InsertBatchAsync(It.IsAny<IEnumerable<LogStatusTracking>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("insert fallido"));

            EtlRunResult result = await h.Create().ExecuteAsync();

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("insert fallido");
            h.Calls.Should().Contain("Rollback").And.NotContain("Commit");
            h.JobControl.Verify(r => r.FailRunAsync(ControlId, CancellationToken.None), Times.Once);
            h.JobControl.Verify(r => r.CompleteRunAsync(It.IsAny<Int64>(), It.IsAny<CancellationToken>()), Times.Never);
            h.Logger.Messages(LogLevel.Error).Should().Contain("La corrida del proceso ETL de logs terminó con error.");
        }

        [Fact]
        public async Task ExecuteAsync_FallaLaSegundaPagina_ConservaLoCargadoDeLaPrimeraYMarcaFailed()
        {
            var h = new Harness();
            h.Pages.Add(Harness.Page("L1"));
            h.Pages.Add(Harness.Page("L2"));
            h.LogRepository.SetupSequence(r => r.InsertBatchAsync(It.IsAny<IEnumerable<LogStatusTracking>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1)
                .ThrowsAsync(new InvalidOperationException("segunda falla"));

            EtlRunResult result = await h.Create().ExecuteAsync();

            result.Success.Should().BeFalse();
            result.LoadedRecords.Should().Be(1);
            h.Calls.Count(c => c == "Commit").Should().Be(1);
            h.Calls.Count(c => c == "Rollback").Should().Be(1);
        }

        [Fact]
        public async Task ExecuteAsync_FallaLaExtraccion_MarcaFailed()
        {
            var h = new Harness();
            h.Gateway.Setup(g => g.FetchDataPagedAsync(It.IsAny<String>(), It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>()))
                .Returns(AsyncSequence.Throwing<DynamicDataSet>(new HttpRequestException("sin red")));

            EtlRunResult result = await h.Create().ExecuteAsync();

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("sin red");
            h.Calls.Should().Contain($"Fail:{ControlId}");
        }

        [Fact]
        public async Task ExecuteAsync_FallaElMapeo_RevierteSinTransaccionYMarcaFailed()
        {
            var h = new Harness();
            h.Pages.Add(Harness.Page("L1"));
            h.Mapping.Setup(m => m.Map(It.IsAny<DynamicDataSet>())).Throws(new FormatException("fecha invalida"));

            EtlRunResult result = await h.Create().ExecuteAsync();

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("fecha invalida");
            h.Calls.Should().NotContain("Begin");
            h.Calls.Should().Contain($"Fail:{ControlId}");
        }

        [Fact]
        public async Task ExecuteAsync_FallaAntesDeIniciarElRegistro_NoIntentaMarcarFailed()
        {
            var h = new Harness();
            h.JobControl.Setup(r => r.StartRunAsync(It.IsAny<EtlJobName>(), It.IsAny<Int32?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("no se pudo iniciar"));

            EtlRunResult result = await h.Create().ExecuteAsync();

            result.Success.Should().BeFalse();
            h.JobControl.Verify(r => r.FailRunAsync(It.IsAny<Int64>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_FallaHasCompletedRun_ReportaErrorSinMarcarFailed()
        {
            var h = new Harness();
            h.JobControl.Setup(r => r.HasCompletedRunAsync(It.IsAny<EtlJobName>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("bd caida"));

            EtlRunResult result = await h.Create().ExecuteAsync();

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("bd caida");
            h.JobControl.Verify(r => r.FailRunAsync(It.IsAny<Int64>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_FallaYTambienFallaMarcarFailed_NoPropagaYLoRegistra()
        {
            var h = new Harness();
            h.Pages.Add(Harness.Page("L1"));
            h.LogRepository.Setup(r => r.InsertBatchAsync(It.IsAny<IEnumerable<LogStatusTracking>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("principal"));
            h.JobControl.Setup(r => r.FailRunAsync(It.IsAny<Int64>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("secundaria"));

            EtlRunResult result = await h.Create().ExecuteAsync();

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("principal");
            h.Logger.Messages(LogLevel.Error).Should().Contain("No se pudo marcar el registro de etl_job_control como FAILED.");
        }

        [Fact]
        public async Task ExecuteAsync_TokenCancelado_TerminaConErrorYMarcaFailedConTokenNone()
        {
            var h = new Harness();
            h.Pages.Add(Harness.Page("L1"));
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            EtlRunResult result = await h.Create().ExecuteAsync(cts.Token);

            result.Success.Should().BeFalse();
            h.Calls.Should().NotContain("Begin");
            h.JobControl.Verify(r => r.FailRunAsync(ControlId, CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ReenviaElTokenAlaPasarelaYAlRepositorio()
        {
            var h = new Harness();
            h.Pages.Add(Harness.Page("L1"));
            using var cts = new CancellationTokenSource();

            await h.Create().ExecuteAsync(cts.Token);

            h.Gateway.Verify(g => g.FetchDataPagedAsync("DATALOGS", It.IsAny<IDictionary<String, String>>(), cts.Token), Times.Once);
            h.LogRepository.Verify(r => r.InsertBatchAsync(It.IsAny<IEnumerable<LogStatusTracking>>(), cts.Token), Times.Once);
            h.JobControl.Verify(r => r.HasCompletedRunAsync(EtlJobName.LogStatusTracking, cts.Token), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_Exitoso_RegistraLogsInformativos()
        {
            var h = new Harness();
            h.Pages.Add(Harness.Page("L1"));

            await h.Create().ExecuteAsync();

            h.Logger.Messages(LogLevel.Information).Should().Contain("Iniciando corrida del proceso ETL de logs (DATALOGS).");
            h.Logger.Messages(LogLevel.Information).Should().Contain(m => m.StartsWith("Corrida ETL de logs finalizada"));
            h.Logger.Entries.Should().NotContain(e => e.Level == LogLevel.Error);
        }
    }
}
