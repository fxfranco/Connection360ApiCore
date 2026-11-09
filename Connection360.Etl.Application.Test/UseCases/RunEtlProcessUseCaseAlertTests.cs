using Connection360.Etl.Application.DTOs;
using Connection360.Etl.Application.Tests.Support;
using Connection360.Etl.Application.UseCases;
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
    /// <summary>
    /// Alerta (event_type "Alert" en outbox_messages) cuando un error impide consultar las APIs externas del proceso ETL.
    /// </summary>
    public class RunEtlProcessUseCaseAlertTests
    {
        private static void FailExtraction(RunEtlHarness h, Exception error, params DynamicDataSet[] pagesBeforeFailure)
        {
            h.Gateway.Setup(g => g.FetchDataPagedAsync("BPMS", It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>()))
                .Returns(AsyncSequence.Throwing(error, pagesBeforeFailure));
        }

        private static EtlOutboxPayload PayloadOf(OutboxMessage message) => JsonSerializer.Deserialize<EtlOutboxPayload>(message.Payload)!;

        [Fact]
        public async Task ExecuteAsync_FallaLaExtraccion_RegistraUnaAlertaEnOutbox()
        {
            var h = new RunEtlHarness(true);
            FailExtraction(h, new HttpRequestException("El API externo respondió con status 500"));

            EtlRunResult result = await h.CreateUseCase().ExecuteAsync();

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("El API externo respondió con status 500");
            OutboxMessage alert = h.InsertedOutboxMessages.Should().ContainSingle().Subject;
            alert.EventType.Should().Be("Alert");
            EtlOutboxPayload payload = PayloadOf(alert);
            payload.EventType.Should().Be("Alert");
            payload.Title.Should().Be("Titulo-Alerta");
            payload.Message.Should().Be("Mensaje-Alerta-El API externo respondió con status 500");
            payload.ClientId.Should().BeEmpty();
            payload.NotificationChannel.Application.Should().BeTrue();
            payload.NotificationChannel.Email.Should().BeTrue();
            payload.NotificationChannel.TextMessages.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteAsync_FallaLaExtraccion_MantieneElManejoActualDelError()
        {
            var h = new RunEtlHarness(true);
            FailExtraction(h, new HttpRequestException("api caida"));

            EtlRunResult result = await h.CreateUseCase().ExecuteAsync();

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("api caida");
            h.Calls.Should().Contain($"Fail:{RunEtlHarness.SheetControlId}");
            h.Logger.Messages(LogLevel.Error).Should().Contain("La corrida del proceso ETL terminó con error.");
        }

        [Fact]
        public async Task ExecuteAsync_FallaLaExtraccion_LaAlertaSeRegistraDespuesDeMarcarFailed()
        {
            var h = new RunEtlHarness(true);
            FailExtraction(h, new HttpRequestException("api caida"));

            await h.CreateUseCase().ExecuteAsync();

            h.Calls.Should().ContainInOrder($"Fail:{RunEtlHarness.SheetControlId}", "InsertOutbox");
        }

        [Fact]
        public async Task ExecuteAsync_FallaLaExtraccionEnMigracionInicial_TambienRegistraLaAlerta()
        {
            var h = new RunEtlHarness(false);
            FailExtraction(h, new InvalidOperationException("Respuesta vacía del API externo."));

            EtlRunResult result = await h.CreateUseCase().ExecuteAsync();

            result.Success.Should().BeFalse();
            h.Calls.Should().Contain($"Fail:{RunEtlHarness.MigrationControlId}");
            h.InsertedOutboxMessages.Should().ContainSingle().Which.EventType.Should().Be("Alert");
        }

        [Fact]
        public async Task ExecuteAsync_FallaLaExtraccionEnUnaPaginaPosterior_RegistraLaAlertaYConservaLoCargado()
        {
            var h = new RunEtlHarness(true);
            FailExtraction(h, new HttpRequestException("El API externo respondió con status 502"), DataSets.WithDocuments("H1"));

            EtlRunResult result = await h.CreateUseCase().ExecuteAsync();

            result.Success.Should().BeFalse();
            h.UpsertedBatches.Should().HaveCount(1);
            h.InsertedOutboxMessages.Should().ContainSingle().Which.EventType.Should().Be("Alert");
        }

        [Fact]
        public async Task ExecuteAsync_FallaLaPasarelaAlCrearLaSecuencia_RegistraLaAlerta()
        {
            var h = new RunEtlHarness(true);
            h.Gateway.Setup(g => g.FetchDataPagedAsync("SIM", It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>()))
                .Throws(new ArgumentException("La API 'SIM' no existe en el appsettings."));

            EtlRunResult result = await h.CreateUseCase().ExecuteAsync();

            result.Success.Should().BeFalse();
            EtlOutboxPayload payload = PayloadOf(h.InsertedOutboxMessages.Should().ContainSingle().Subject);
            payload.Message.Should().Be("Mensaje-Alerta-La API \"SIM\" no existe en el appsettings.");
        }

        [Fact]
        public async Task ExecuteAsync_FallaLaExtraccion_ElMensajeNoExponeDatosSensibles()
        {
            var h = new RunEtlHarness(true);
            FailExtraction(h, new InvalidOperationException("No se pudo leer https://api.interna.com/v1?token=abc123 con password=Clave123 desde 10.0.0.5:443"));

            await h.CreateUseCase().ExecuteAsync();

            String message = PayloadOf(h.InsertedOutboxMessages.Single()).Message;
            message.Should().NotContain("interna.com").And.NotContain("abc123").And.NotContain("Clave123").And.NotContain("10.0.0.5");
            message.Should().Contain("[url]");
        }

        [Fact]
        public async Task ExecuteAsync_TiempoDeEsperaDelHttpClient_RegistraLaAlertaConElMotivo()
        {
            var h = new RunEtlHarness(true);
            FailExtraction(h, new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout of 100 seconds elapsing."));

            EtlRunResult result = await h.CreateUseCase().ExecuteAsync();

            result.Success.Should().BeFalse();
            PayloadOf(h.InsertedOutboxMessages.Should().ContainSingle().Subject).Message
                .Should().Be($"Mensaje-Alerta-{EtlErrorSanitizer.TimeoutReason}");
        }

        [Fact]
        public async Task ExecuteAsync_CancelacionPedidaPorElProceso_NoRegistraAlerta()
        {
            var h = new RunEtlHarness(true);
            using var cts = new CancellationTokenSource();
            FailExtraction(h, new OperationCanceledException(cts.Token));
            cts.Cancel();

            EtlRunResult result = await h.CreateUseCase().ExecuteAsync(cts.Token);

            result.Success.Should().BeFalse();
            h.InsertedOutboxMessages.Should().BeEmpty();
            h.OutboxRepository.Verify(r => r.InsertBatchAsync(It.IsAny<IEnumerable<OutboxMessage>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_FallaLaCarga_NoRegistraAlertaDeExtraccion()
        {
            var h = new RunEtlHarness(true).WithPages("BPMS", DataSets.WithDocuments("H1"));
            h.SheetRepository.Setup(r => r.UpsertBatchAsync(It.IsAny<IEnumerable<ApplicationDataSheet>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("boom upsert"));

            EtlRunResult result = await h.CreateUseCase().ExecuteAsync();

            result.Success.Should().BeFalse();
            h.InsertedOutboxMessages.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteAsync_FallaLaTransformacion_NoRegistraAlertaDeExtraccion()
        {
            var h = new RunEtlHarness(true).WithPages("BPMS", DataSets.WithDocuments("H1"));
            h.Merger.Setup(m => m.Merge(It.IsAny<IEnumerable<DynamicDataSet>>(), It.IsAny<String>(), It.IsAny<DataSetJoinType>()))
                .Throws(new InvalidOperationException("merge roto"));

            EtlRunResult result = await h.CreateUseCase().ExecuteAsync();

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("merge roto");
            h.InsertedOutboxMessages.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteAsync_FallaAntesDeConsultarLasApis_NoRegistraAlertaDeExtraccion()
        {
            var h = new RunEtlHarness(false);
            h.JobControl.Setup(r => r.HasCompletedRunAsync(EtlJobName.ApplicationDataSheetMigration, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("bd no disponible"));

            await h.CreateUseCase().ExecuteAsync();

            h.InsertedOutboxMessages.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteAsync_CorridaExitosa_NoRegistraAlerta()
        {
            var h = new RunEtlHarness(true).WithPages("BPMS", DataSets.WithDocuments("H1"));

            EtlRunResult result = await h.CreateUseCase().ExecuteAsync();

            result.Success.Should().BeTrue();
            h.InsertedOutboxMessages.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteAsync_FallaElRegistroDeLaAlerta_NoCambiaElResultadoYSeDejaEnElLog()
        {
            var h = new RunEtlHarness(true);
            FailExtraction(h, new HttpRequestException("api caida"));
            h.OutboxRepository.Setup(r => r.InsertBatchAsync(It.IsAny<IEnumerable<OutboxMessage>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("outbox caido"));

            EtlRunResult result = await h.CreateUseCase().ExecuteAsync();

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("api caida");
            h.Logger.Messages(LogLevel.Error).Should().Contain("No se pudo registrar en outbox_messages la alerta del error de extracción del proceso ETL.");
        }

        [Fact]
        public async Task ExecuteAsync_LaAlertaSeRegistraConTokenNone()
        {
            var h = new RunEtlHarness(true);
            using var cts = new CancellationTokenSource();
            FailExtraction(h, new HttpRequestException("api caida"));

            await h.CreateUseCase().ExecuteAsync(cts.Token);

            h.OutboxRepository.Verify(r => r.InsertBatchAsync(It.IsAny<IEnumerable<OutboxMessage>>(), CancellationToken.None), Times.Once);
        }
    }
}
