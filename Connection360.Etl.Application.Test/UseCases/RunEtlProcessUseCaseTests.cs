using Connection360.Etl.Application.DTOs;
using Connection360.Etl.Application.Tests.Support;
using Connection360.Etl.Domain.Constants;
using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Domain.Enums;
using Connection360.Etl.Domain.Ports.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace Connection360.Etl.Application.Tests.UseCases
{
    public class RunEtlProcessUseCaseTests
    {
        private static readonly String[] OperationalApis = { "BPMS", "SIM", "OPENCOMEX", "ASISCOMEX", "SYSTEMCARRIER" };

        private static ApplicationDataSheetChange Change(
            String document, Boolean isNew = false, Boolean stateChanged = false, Boolean commentChanged = false,
            Int64 idOperacion = 0, String estadoAnterior = "", String nuevoEstado = "")
            => new()
            {
                DocumentoTransporteHbl = document,
                NitCliente = "NIT-" + document,
                IsNewDocument = isNew,
                StateChanged = stateChanged,
                CommentChanged = commentChanged,
                IdOperacion = idOperacion,
                EstadoAnterior = estadoAnterior,
                NuevoEstado = nuevoEstado,
            };

        // ---------------------------------------------------------------- migración inicial

        [Fact]
        public async Task ExecuteAsync_SinMigracionPrevia_EjecutaCargaInicialSinDetectarCambios()
        {
            var h = new RunEtlHarness(migrationAlreadyCompleted: false)
                .WithPages("BPMS", DataSets.WithDocuments("HBL1", "HBL2"))
                .WithPages("SIM", DataSets.WithDocuments("HBL1"));

            EtlRunResult result = await h.CreateUseCase(pageSize: 500).ExecuteAsync();

            result.Success.Should().BeTrue();
            result.ErrorMessage.Should().BeNull();
            result.IsInitialMigrationRun.Should().BeTrue();
            result.StateChangesDetected.Should().Be(0);
            result.CommentChangesDetected.Should().Be(0);
            result.TransformedRecords.Should().Be(3);
            result.LoadedRecords.Should().Be(3);
            result.ExtractedRecordsByApi["BPMS"].Should().Be(2);
            result.ExtractedRecordsByApi["SIM"].Should().Be(1);
            result.FinishedAtUtc.Should().BeOnOrAfter(result.StartedAtUtc);

            h.SheetRepository.Verify(r => r.GetChangeSnapshotsAsync(It.IsAny<IEnumerable<String>>(), It.IsAny<CancellationToken>()), Times.Never);
            h.SheetRepository.Verify(r => r.GetIdsByDocumentAsync(It.IsAny<IEnumerable<String>>(), It.IsAny<CancellationToken>()), Times.Never);
            h.Detector.Verify(d => d.DetectChanges(It.IsAny<IReadOnlyList<ApplicationDataSheet>>(), It.IsAny<IReadOnlyDictionary<String, ApplicationDataSheetChangeSnapshot>>()), Times.Never);
            h.LogRepository.Verify(r => r.InsertBatchAsync(It.IsAny<IEnumerable<LogStatusTracking>>(), It.IsAny<CancellationToken>()), Times.Never);
            h.OutboxRepository.Verify(r => r.InsertBatchAsync(It.IsAny<IEnumerable<OutboxMessage>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_SinMigracionPrevia_RegistraControlDeAmbosJobsEnElOrdenEsperado()
        {
            var h = new RunEtlHarness(migrationAlreadyCompleted: false)
                .WithPages("BPMS", DataSets.WithDocuments("HBL1", "HBL2"));

            await h.CreateUseCase(pageSize: 500).ExecuteAsync();

            h.Calls.Should().Equal(
                $"Start:{EtlJobName.ApplicationDataSheetMigration}:500",
                $"Start:{EtlJobName.ApplicationDataSheet}:500",
                "Begin",
                "Upsert",
                $"Progress:{RunEtlHarness.SheetControlId}:1:2",
                $"Progress:{RunEtlHarness.MigrationControlId}:1:2",
                "Commit",
                $"Complete:{RunEtlHarness.SheetControlId}",
                $"Complete:{RunEtlHarness.MigrationControlId}");
        }

        [Fact]
        public async Task ExecuteAsync_SinMigracionPrevia_PaginacionDeshabilitadaGuardaPageSizeNulo()
        {
            var h = new RunEtlHarness(migrationAlreadyCompleted: false)
                .WithPages("BPMS", DataSets.WithDocuments("HBL1"));

            await h.CreateUseCase(pageSize: null).ExecuteAsync();

            h.JobControl.Verify(r => r.StartRunAsync(EtlJobName.ApplicationDataSheetMigration, null, It.IsAny<CancellationToken>()), Times.Once);
            h.JobControl.Verify(r => r.StartRunAsync(EtlJobName.ApplicationDataSheet, null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ConMigracionPrevia_NoRegistraElJobDeMigracionNiLoCompleta()
        {
            var h = new RunEtlHarness(migrationAlreadyCompleted: true)
                .WithPages("BPMS", DataSets.WithDocuments("HBL1"));

            EtlRunResult result = await h.CreateUseCase(pageSize: 10).ExecuteAsync();

            result.Success.Should().BeTrue();
            result.IsInitialMigrationRun.Should().BeFalse();
            h.JobControl.Verify(r => r.StartRunAsync(EtlJobName.ApplicationDataSheetMigration, It.IsAny<Int32?>(), It.IsAny<CancellationToken>()), Times.Never);
            h.JobControl.Verify(r => r.CompleteRunAsync(RunEtlHarness.MigrationControlId, It.IsAny<CancellationToken>()), Times.Never);
            h.JobControl.Verify(r => r.RegisterPageProgressAsync(RunEtlHarness.MigrationControlId, It.IsAny<Int32>(), It.IsAny<Int32>(), It.IsAny<CancellationToken>()), Times.Never);
            h.JobControl.Verify(r => r.CompleteRunAsync(RunEtlHarness.SheetControlId, It.IsAny<CancellationToken>()), Times.Once);
        }

        // ---------------------------------------------------------------- extracción / transformación

        [Fact]
        public async Task ExecuteAsync_Extraccion_ConsultaLasCincoApisOperativasSinFiltros()
        {
            var h = new RunEtlHarness(true);

            await h.CreateUseCase().ExecuteAsync();

            h.FetchRequests.Select(r => r.Api).Should().Equal(OperationalApis);
            h.FetchRequests.Should().OnlyContain(r => r.Filters.Count == 0);
        }

        [Fact]
        public async Task ExecuteAsync_Transformacion_CombinaPorDocumentoConFullOuterEnElOrdenDeLasApis()
        {
            var bpms = DataSets.WithDocuments("A");
            var sim = DataSets.WithDocuments("B");
            var h = new RunEtlHarness(true).WithPages("BPMS", bpms).WithPages("SIM", sim);

            await h.CreateUseCase().ExecuteAsync();

            h.MergeCalls.Should().HaveCount(1);
            var call = h.MergeCalls[0];
            call.JoinField.Should().Be(ExternalDataFields.DocumentNumber);
            call.JoinType.Should().Be(DataSetJoinType.FullOuter);
            call.DataSets.Should().HaveCount(5);
            call.DataSets[0].Should().BeSameAs(bpms);
            call.DataSets[1].Should().BeSameAs(sim);
            call.DataSets.Skip(2).Should().OnlyContain(d => d.Rows.Count == 0);
        }

        [Fact]
        public async Task ExecuteAsync_VariasRondas_CargaCadaRondaEnSuPropiaTransaccion()
        {
            var h = new RunEtlHarness(true)
                .WithPages("BPMS", DataSets.WithDocuments("A1", "A2"), DataSets.WithDocuments("A3"))
                .WithPages("SIM", DataSets.WithDocuments("S1"));

            EtlRunResult result = await h.CreateUseCase(pageSize: 2).ExecuteAsync();

            result.Success.Should().BeTrue();
            result.LoadedRecords.Should().Be(4);
            result.TransformedRecords.Should().Be(4);
            result.ExtractedRecordsByApi["BPMS"].Should().Be(3);
            result.ExtractedRecordsByApi["SIM"].Should().Be(1);
            h.Calls.Count(c => c == "Begin").Should().Be(2);
            h.Calls.Count(c => c == "Commit").Should().Be(2);
            h.Calls.Should().NotContain("Rollback");
            h.UpsertedBatches.Select(b => b.Count).Should().Equal(3, 1);
        }

        [Fact]
        public async Task ExecuteAsync_ApisSinDatos_NoAcumulaClavesEnLosRegistrosExtraidosDeEsasApis()
        {
            var h = new RunEtlHarness(true).WithPages("BPMS", DataSets.WithDocuments("A"));

            EtlRunResult result = await h.CreateUseCase().ExecuteAsync();

            result.ExtractedRecordsByApi.Keys.Should().Contain("BPMS");
            result.ExtractedRecordsByApi["SIM"].Should().Be(0);
            result.ExtractedRecordsByApi.Should().HaveCount(5);
            result.ExtractedRecordsByApi.ContainsKey("bpms").Should().BeTrue("el diccionario es insensible a mayúsculas");
        }

        [Fact]
        public async Task ExecuteAsync_SinNingunaPagina_CompletaConExitoSinCargarNada()
        {
            var h = new RunEtlHarness(true);

            EtlRunResult result = await h.CreateUseCase().ExecuteAsync();

            result.Success.Should().BeTrue();
            result.LoadedRecords.Should().Be(0);
            result.TransformedRecords.Should().Be(0);
            h.Calls.Should().NotContain("Begin");
            h.JobControl.Verify(r => r.CompleteRunAsync(RunEtlHarness.SheetControlId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_RondaConTodasLasApisVacias_DetieneLaIteracionSinCargar()
        {
            // Todas las APIs entregan una página (sin filas) en la ronda 1: no queda nada que procesar.
            var empty = DataSets.WithDocuments();
            var h = new RunEtlHarness(true);
            foreach (String api in OperationalApis)
                h.WithPages(api, empty, DataSets.WithDocuments("NUNCA_SE_PROCESA"));

            EtlRunResult result = await h.CreateUseCase().ExecuteAsync();

            result.Success.Should().BeTrue();
            result.LoadedRecords.Should().Be(0);
            h.UpsertedBatches.Should().BeEmpty();
            h.MergeCalls.Should().BeEmpty();
        }

        // ---------------------------------------------------------------- detección de cambios

        [Fact]
        public async Task ExecuteAsync_SinCambiosDetectados_NoNotificaPeroRegistraAvance()
        {
            var h = new RunEtlHarness(true).WithPages("BPMS", DataSets.WithDocuments("HBL1"));

            EtlRunResult result = await h.CreateUseCase(pageSize: 5).ExecuteAsync();

            result.StateChangesDetected.Should().Be(0);
            result.CommentChangesDetected.Should().Be(0);
            h.Calls.Should().Equal(
                $"Start:{EtlJobName.ApplicationDataSheet}:5",
                "Begin", "Snapshots", "Detect", "Upsert",
                $"Progress:{RunEtlHarness.SheetControlId}:1:1",
                "Commit",
                $"Complete:{RunEtlHarness.SheetControlId}");
            h.SheetRepository.Verify(r => r.GetIdsByDocumentAsync(It.IsAny<IEnumerable<String>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_ConsultaSnapshotsAcotadosALosDocumentosDeLaRondaAntesDelUpsert()
        {
            var h = new RunEtlHarness(true).WithPages("BPMS", DataSets.WithDocuments("HBL1", "HBL2"));
            List<String>? requested = null;
            h.SheetRepository.Setup(r => r.GetChangeSnapshotsAsync(It.IsAny<IEnumerable<String>>(), It.IsAny<CancellationToken>()))
                .Callback((IEnumerable<String> docs, CancellationToken _) => { requested = docs.ToList(); h.Calls.Add("Snapshots"); })
                .ReturnsAsync(new Dictionary<String, ApplicationDataSheetChangeSnapshot>());

            await h.CreateUseCase().ExecuteAsync();

            requested.Should().Equal("HBL1", "HBL2");
            h.Calls.IndexOf("Snapshots").Should().BeLessThan(h.Calls.IndexOf("Upsert"));
        }

        [Fact]
        public async Task ExecuteAsync_PasaAlDetectorLasFilasActualesYLosSnapshotsPrevios()
        {
            var h = new RunEtlHarness(true).WithPages("BPMS", DataSets.WithDocuments("HBL1"));
            var snapshots = new Dictionary<String, ApplicationDataSheetChangeSnapshot>
            {
                ["HBL1"] = new() { Id = 7, DocumentoTransporteHbl = "HBL1", Estado = "VIEJO" }
            };
            h.SheetRepository.Setup(r => r.GetChangeSnapshotsAsync(It.IsAny<IEnumerable<String>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(snapshots);

            await h.CreateUseCase().ExecuteAsync();

            h.Detector.Verify(d => d.DetectChanges(
                It.Is<IReadOnlyList<ApplicationDataSheet>>(s => s.Count == 1 && s[0].DocumentoTransporteHbl == "HBL1"),
                It.Is<IReadOnlyDictionary<String, ApplicationDataSheetChangeSnapshot>>(d => ReferenceEquals(d, snapshots))), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ConCambioDeEstado_InsertaLogYOutboxYContabilizaElCambio()
        {
            var h = new RunEtlHarness(true).WithPages("BPMS", DataSets.WithDocuments("HBL1"));
            h.Detector.Setup(d => d.DetectChanges(It.IsAny<IReadOnlyList<ApplicationDataSheet>>(), It.IsAny<IReadOnlyDictionary<String, ApplicationDataSheetChangeSnapshot>>()))
                .Returns(new List<ApplicationDataSheetChange>
                {
                    Change("HBL1", stateChanged: true, idOperacion: 55, estadoAnterior: "EN TRANSITO", nuevoEstado: "ENTREGADO")
                });

            EtlRunResult result = await h.CreateUseCase(systemUser: "ROBOT").ExecuteAsync();

            result.Success.Should().BeTrue();
            result.StateChangesDetected.Should().Be(1);
            result.CommentChangesDetected.Should().Be(0);

            LogStatusTracking log = h.InsertedLogRows.Should().ContainSingle().Subject;
            log.IdOperacion.Should().Be(55);
            log.DocumentoTransporteHbl.Should().Be("HBL1");
            log.UsuarioCambio.Should().Be("ROBOT");
            log.Mensaje.Should().Be("Mensaje-ENTREGADO");
            log.EstadoAnterior.Should().Be("EN TRANSITO");
            log.NuevoEstado.Should().Be("ENTREGADO");
            log.FechaCambio.Kind.Should().Be(DateTimeKind.Utc);

            OutboxMessage outbox = h.InsertedOutboxMessages.Should().ContainSingle().Subject;
            outbox.EventType.Should().Be("ChangeState");
            outbox.Id.Should().NotBe(Guid.Empty);
            outbox.CreatedAt.Should().Be(log.FechaCambio);

            EtlOutboxPayload payload = JsonSerializer.Deserialize<EtlOutboxPayload>(outbox.Payload)!;
            payload.ClientId.Should().Be("NIT-HBL1");
            payload.EventType.Should().Be("ChangeState");
            payload.DocumentNumber.Should().Be("HBL1");
            payload.Title.Should().Be("Titulo-ENTREGADO");
            payload.Message.Should().Be("Mensaje-ENTREGADO");
            payload.MessageDate.Should().Be(outbox.CreatedAt);
        }

        [Fact]
        public async Task ExecuteAsync_ConCambioDeComentario_InsertaSoloOutboxSinLog()
        {
            var h = new RunEtlHarness(true).WithPages("BPMS", DataSets.WithDocuments("HBL1"));
            h.Detector.Setup(d => d.DetectChanges(It.IsAny<IReadOnlyList<ApplicationDataSheet>>(), It.IsAny<IReadOnlyDictionary<String, ApplicationDataSheetChangeSnapshot>>()))
                .Returns(new List<ApplicationDataSheetChange> { Change("HBL1", commentChanged: true, idOperacion: 9) });

            EtlRunResult result = await h.CreateUseCase().ExecuteAsync();

            result.StateChangesDetected.Should().Be(0);
            result.CommentChangesDetected.Should().Be(1);
            h.InsertedLogRows.Should().BeEmpty();
            h.LogRepository.Verify(r => r.InsertBatchAsync(It.IsAny<IEnumerable<LogStatusTracking>>(), It.IsAny<CancellationToken>()), Times.Never);

            OutboxMessage outbox = h.InsertedOutboxMessages.Should().ContainSingle().Subject;
            outbox.EventType.Should().Be("Comment");
            EtlOutboxPayload payload = JsonSerializer.Deserialize<EtlOutboxPayload>(outbox.Payload)!;
            payload.EventType.Should().Be("Comment");
            payload.Title.Should().Be("Titulo-Comentario");
            payload.Message.Should().Be("Mensaje-Comentario");
            payload.ClientId.Should().Be("NIT-HBL1");
            h.Catalog.Verify(c => c.GetStateChangeMessage(It.IsAny<String>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_ConCambioDeEstadoYComentarioEnElMismoDocumento_GeneraDosMensajesDeOutboxYUnLog()
        {
            var h = new RunEtlHarness(true).WithPages("BPMS", DataSets.WithDocuments("HBL1"));
            h.Detector.Setup(d => d.DetectChanges(It.IsAny<IReadOnlyList<ApplicationDataSheet>>(), It.IsAny<IReadOnlyDictionary<String, ApplicationDataSheetChangeSnapshot>>()))
                .Returns(new List<ApplicationDataSheetChange>
                {
                    Change("HBL1", stateChanged: true, commentChanged: true, nuevoEstado: "X")
                });

            EtlRunResult result = await h.CreateUseCase().ExecuteAsync();

            result.StateChangesDetected.Should().Be(1);
            result.CommentChangesDetected.Should().Be(1);
            h.InsertedLogRows.Should().HaveCount(1);
            h.InsertedOutboxMessages.Select(m => m.EventType).Should().Equal("ChangeState", "Comment");
        }

        [Fact]
        public async Task ExecuteAsync_ConVariosCambios_InsertaUnLotePorTablaYSumaContadores()
        {
            var h = new RunEtlHarness(true).WithPages("BPMS", DataSets.WithDocuments("H1", "H2", "H3"));
            h.Detector.Setup(d => d.DetectChanges(It.IsAny<IReadOnlyList<ApplicationDataSheet>>(), It.IsAny<IReadOnlyDictionary<String, ApplicationDataSheetChangeSnapshot>>()))
                .Returns(new List<ApplicationDataSheetChange>
                {
                    Change("H1", stateChanged: true),
                    Change("H2", commentChanged: true),
                    Change("H3", stateChanged: true, commentChanged: true),
                });

            EtlRunResult result = await h.CreateUseCase().ExecuteAsync();

            result.StateChangesDetected.Should().Be(2);
            result.CommentChangesDetected.Should().Be(2);
            h.LogRepository.Verify(r => r.InsertBatchAsync(It.IsAny<IEnumerable<LogStatusTracking>>(), It.IsAny<CancellationToken>()), Times.Once);
            h.OutboxRepository.Verify(r => r.InsertBatchAsync(It.IsAny<IEnumerable<OutboxMessage>>(), It.IsAny<CancellationToken>()), Times.Once);
            h.InsertedLogRows.Should().HaveCount(2);
            h.InsertedOutboxMessages.Should().HaveCount(4);
            h.Calls.Should().ContainInOrder("Begin", "Snapshots", "Upsert", "InsertLog", "InsertOutbox", $"Progress:{RunEtlHarness.SheetControlId}:1:3", "Commit");
        }

        [Fact]
        public async Task ExecuteAsync_ConDocumentosNuevos_ResuelveSusIdsTrasElUpsertSoloParaEsosDocumentos()
        {
            var h = new RunEtlHarness(true).WithPages("BPMS", DataSets.WithDocuments("NUEVO", "EXISTENTE", "SIN_ID"));
            var changes = new List<ApplicationDataSheetChange>
            {
                Change("NUEVO", isNew: true, stateChanged: true),
                Change("EXISTENTE", isNew: false, stateChanged: true, idOperacion: 77),
                Change("SIN_ID", isNew: true, commentChanged: true),
            };
            h.Detector.Setup(d => d.DetectChanges(It.IsAny<IReadOnlyList<ApplicationDataSheet>>(), It.IsAny<IReadOnlyDictionary<String, ApplicationDataSheetChangeSnapshot>>()))
                .Returns(changes);
            List<String>? requestedIds = null;
            h.SheetRepository.Setup(r => r.GetIdsByDocumentAsync(It.IsAny<IEnumerable<String>>(), It.IsAny<CancellationToken>()))
                .Callback((IEnumerable<String> docs, CancellationToken _) => { requestedIds = docs.ToList(); h.Calls.Add("GetIds"); })
                .ReturnsAsync(new Dictionary<String, Int64> { ["NUEVO"] = 1234 });

            await h.CreateUseCase().ExecuteAsync();

            requestedIds.Should().Equal("NUEVO", "SIN_ID");
            h.Calls.IndexOf("GetIds").Should().BeGreaterThan(h.Calls.IndexOf("Upsert"));
            h.Calls.IndexOf("GetIds").Should().BeLessThan(h.Calls.IndexOf("InsertLog"));
            changes[0].IdOperacion.Should().Be(1234);
            changes[1].IdOperacion.Should().Be(77, "los documentos existentes conservan el Id del snapshot");
            changes[2].IdOperacion.Should().Be(0, "si no se resuelve el Id, queda sin modificar");
            h.InsertedLogRows.Select(l => (l.DocumentoTransporteHbl, l.IdOperacion)).Should().BeEquivalentTo(new[] { ("NUEVO", 1234L), ("EXISTENTE", 77L) });
        }

        [Fact]
        public async Task ExecuteAsync_CambiosSoloEnDocumentosExistentes_NoConsultaIds()
        {
            var h = new RunEtlHarness(true).WithPages("BPMS", DataSets.WithDocuments("H1"));
            h.Detector.Setup(d => d.DetectChanges(It.IsAny<IReadOnlyList<ApplicationDataSheet>>(), It.IsAny<IReadOnlyDictionary<String, ApplicationDataSheetChangeSnapshot>>()))
                .Returns(new List<ApplicationDataSheetChange> { Change("H1", isNew: false, stateChanged: true, idOperacion: 3) });

            await h.CreateUseCase().ExecuteAsync();

            h.SheetRepository.Verify(r => r.GetIdsByDocumentAsync(It.IsAny<IEnumerable<String>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_VariasRondasConCambios_AcumulaContadoresDeTodasLasRondas()
        {
            var h = new RunEtlHarness(true)
                .WithPages("BPMS", DataSets.WithDocuments("R1"), DataSets.WithDocuments("R2"));
            h.Detector.SetupSequence(d => d.DetectChanges(It.IsAny<IReadOnlyList<ApplicationDataSheet>>(), It.IsAny<IReadOnlyDictionary<String, ApplicationDataSheetChangeSnapshot>>()))
                .Returns(new List<ApplicationDataSheetChange> { Change("R1", stateChanged: true) })
                .Returns(new List<ApplicationDataSheetChange> { Change("R2", commentChanged: true) });

            EtlRunResult result = await h.CreateUseCase().ExecuteAsync();

            result.StateChangesDetected.Should().Be(1);
            result.CommentChangesDetected.Should().Be(1);
            h.Calls.Should().ContainInOrder(
                $"Progress:{RunEtlHarness.SheetControlId}:1:1", "Commit",
                $"Progress:{RunEtlHarness.SheetControlId}:2:1", "Commit");
        }

        // ---------------------------------------------------------------- fallos

        [Fact]
        public async Task ExecuteAsync_FallaElUpsertEnMigracionInicial_RevierteYMarcaAmbosJobsComoFailed()
        {
            var h = new RunEtlHarness(false).WithPages("BPMS", DataSets.WithDocuments("H1"));
            h.SheetRepository.Setup(r => r.UpsertBatchAsync(It.IsAny<IEnumerable<ApplicationDataSheet>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("boom upsert"));

            EtlRunResult result = await h.CreateUseCase().ExecuteAsync();

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("boom upsert");
            result.IsInitialMigrationRun.Should().BeFalse();
            result.FinishedAtUtc.Should().BeOnOrAfter(result.StartedAtUtc);
            h.Calls.Should().Contain("Rollback").And.NotContain("Commit");
            h.Calls.Should().Contain($"Fail:{RunEtlHarness.SheetControlId}").And.Contain($"Fail:{RunEtlHarness.MigrationControlId}");
            h.JobControl.Verify(r => r.CompleteRunAsync(It.IsAny<Int64>(), It.IsAny<CancellationToken>()), Times.Never);
            h.Logger.Messages(LogLevel.Error).Should().Contain("La corrida del proceso ETL terminó con error.");
        }

        [Fact]
        public async Task ExecuteAsync_FallaSinMigracion_SoloMarcaFailedElJobPrincipal()
        {
            var h = new RunEtlHarness(true).WithPages("BPMS", DataSets.WithDocuments("H1"));
            h.SheetRepository.Setup(r => r.GetChangeSnapshotsAsync(It.IsAny<IEnumerable<String>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TimeoutException("snapshot lento"));

            EtlRunResult result = await h.CreateUseCase().ExecuteAsync();

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("snapshot lento");
            h.JobControl.Verify(r => r.FailRunAsync(RunEtlHarness.SheetControlId, CancellationToken.None), Times.Once);
            h.JobControl.Verify(r => r.FailRunAsync(RunEtlHarness.MigrationControlId, It.IsAny<CancellationToken>()), Times.Never);
            h.Calls.Should().Contain("Rollback");
        }

        [Fact]
        public async Task ExecuteAsync_FallaElNotificador_RevierteLaRondaCompleta()
        {
            var h = new RunEtlHarness(true).WithPages("BPMS", DataSets.WithDocuments("H1"));
            h.Detector.Setup(d => d.DetectChanges(It.IsAny<IReadOnlyList<ApplicationDataSheet>>(), It.IsAny<IReadOnlyDictionary<String, ApplicationDataSheetChangeSnapshot>>()))
                .Returns(new List<ApplicationDataSheetChange> { Change("H1", stateChanged: true) });
            h.OutboxRepository.Setup(r => r.InsertBatchAsync(It.IsAny<IEnumerable<OutboxMessage>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("outbox caido"));

            EtlRunResult result = await h.CreateUseCase().ExecuteAsync();

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("outbox caido");
            h.Calls.Should().Contain("Rollback").And.NotContain("Commit");
            h.JobControl.Verify(r => r.RegisterPageProgressAsync(It.IsAny<Int64>(), It.IsAny<Int32>(), It.IsAny<Int32>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_FallaLaExtraccion_MarcaFailedYReportaElMensaje()
        {
            var h = new RunEtlHarness(true);
            h.Gateway.Setup(g => g.FetchDataPagedAsync("BPMS", It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>()))
                .Returns(AsyncSequence.Throwing<DynamicDataSet>(new HttpRequestException("api caida")));

            EtlRunResult result = await h.CreateUseCase().ExecuteAsync();

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("api caida");
            h.Calls.Should().Contain($"Fail:{RunEtlHarness.SheetControlId}");
        }

        [Fact]
        public async Task ExecuteAsync_FallaHasCompletedRunAntesDeIniciar_NoMarcaFailedPorqueNadaSeRegistro()
        {
            var h = new RunEtlHarness(false);
            h.JobControl.Setup(r => r.HasCompletedRunAsync(EtlJobName.ApplicationDataSheetMigration, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("bd no disponible"));

            EtlRunResult result = await h.CreateUseCase().ExecuteAsync();

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("bd no disponible");
            h.JobControl.Verify(r => r.FailRunAsync(It.IsAny<Int64>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_FallaYTambienFallaMarcarFailed_NoPropagaYLoRegistraEnElLog()
        {
            var h = new RunEtlHarness(false).WithPages("BPMS", DataSets.WithDocuments("H1"));
            h.SheetRepository.Setup(r => r.UpsertBatchAsync(It.IsAny<IEnumerable<ApplicationDataSheet>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("falla principal"));
            h.JobControl.Setup(r => r.FailRunAsync(It.IsAny<Int64>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("falla al marcar"));

            EtlRunResult result = await h.CreateUseCase().ExecuteAsync();

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("falla principal");
            h.Logger.Messages(LogLevel.Error).Should().Contain("No se pudo marcar el registro de etl_job_control como FAILED.");
            h.Logger.Messages(LogLevel.Error).Should().Contain("No se pudo marcar el registro de migración inicial (etl_job_control) como FAILED.");
        }

        [Fact]
        public async Task ExecuteAsync_TokenYaCancelado_TerminaConErrorYMarcaFailedConTokenNone()
        {
            var h = new RunEtlHarness(true).WithPages("BPMS", DataSets.WithDocuments("H1"));
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            EtlRunResult result = await h.CreateUseCase().ExecuteAsync(cts.Token);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
            h.UpsertedBatches.Should().BeEmpty();
            h.JobControl.Verify(r => r.FailRunAsync(RunEtlHarness.SheetControlId, CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ReenviaElTokenDeCancelacionALasDependencias()
        {
            var h = new RunEtlHarness(true).WithPages("BPMS", DataSets.WithDocuments("H1"));
            using var cts = new CancellationTokenSource();

            await h.CreateUseCase().ExecuteAsync(cts.Token);

            h.Gateway.Verify(g => g.FetchDataPagedAsync(It.IsAny<String>(), It.IsAny<IDictionary<String, String>>(), cts.Token), Times.Exactly(5));
            h.SheetRepository.Verify(r => r.UpsertBatchAsync(It.IsAny<IEnumerable<ApplicationDataSheet>>(), cts.Token), Times.Once);
            h.UnitOfWork.Verify(u => u.CommitAsync(cts.Token), Times.Once);
        }

        // ---------------------------------------------------------------- resultado / logging

        [Fact]
        public async Task ExecuteAsync_Exitoso_RegistraLogsInformativosDeLaCorrida()
        {
            var h = new RunEtlHarness(false).WithPages("BPMS", DataSets.WithDocuments("H1"));

            await h.CreateUseCase().ExecuteAsync();

            h.Logger.Messages(LogLevel.Information).Should().Contain("Iniciando corrida del proceso ETL.");
            h.Logger.Messages(LogLevel.Information).Should().Contain(m => m.Contains("Corrida ETL finalizada") && m.Contains("MigraciónInicial=True"));
            h.Logger.Entries.Should().NotContain(e => e.Level == LogLevel.Error);
        }

        [Fact]
        public async Task ExecuteAsync_ResuelveLosRepositoriosDeControlYNotificacionUnaSolaVezPorCorrida()
        {
            var h = new RunEtlHarness(true).WithPages("BPMS", DataSets.WithDocuments("H1"), DataSets.WithDocuments("H2"));

            await h.CreateUseCase().ExecuteAsync();

            h.UnitOfWork.Verify(u => u.GetRepository<IEtlJobControlRepository>(), Times.Once);
            h.UnitOfWork.Verify(u => u.GetRepository<ILogStatusTrackingRepository>(), Times.Once);
            h.UnitOfWork.Verify(u => u.GetRepository<IOutboxMessageRepository>(), Times.Once);
            h.UnitOfWork.Verify(u => u.GetRepository<IApplicationDataSheetRepository>(), Times.Exactly(2));
        }
    }
}
