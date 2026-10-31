using Connection360.Etl.Application.DTOs;
using Connection360.Etl.Application.UseCases;
using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Domain.Interfaces;
using Connection360.Etl.Domain.Ports.Persistence;
using FluentAssertions;
using Moq;
using System.Text.Json;
using Xunit;

namespace Connection360.Etl.Application.Tests.UseCases
{
    public class EtlAlertNotifierTests
    {
        private readonly Mock<IOutboxMessageRepository> _outbox = new();
        private readonly Mock<IEtlChangeMessageCatalog> _catalog = new();
        private readonly List<OutboxMessage> _inserted = new();
        private readonly EtlAlertNotifier _sut;

        public EtlAlertNotifierTests()
        {
            _catalog.Setup(c => c.GetEtlAlertMessage(It.IsAny<String>()))
                .Returns((String error) => ("Alerta en proceso de ETL", $"Se generó un error '{error}' en el proceso de ETL"));
            _outbox.Setup(r => r.InsertBatchAsync(It.IsAny<IEnumerable<OutboxMessage>>(), It.IsAny<CancellationToken>()))
                .Callback((IEnumerable<OutboxMessage> rows, CancellationToken _) => _inserted.AddRange(rows))
                .ReturnsAsync((IEnumerable<OutboxMessage> rows, CancellationToken _) => rows.Count());
            _sut = new EtlAlertNotifier(_outbox.Object, _catalog.Object);
        }

        [Fact]
        public async Task NotifyExtractionFailureAsync_InsertaUnSoloMensajeDeTipoAlert()
        {
            OutboxMessage message = await _sut.NotifyExtractionFailureAsync(new HttpRequestException("El API externo respondió con status 500"), CancellationToken.None);

            _inserted.Should().ContainSingle().Which.Should().BeSameAs(message);
            message.EventType.Should().Be("Alert");
            message.Id.Should().NotBe(Guid.Empty);
            message.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Fact]
        public async Task NotifyExtractionFailureAsync_EnviaAlCatalogoElErrorSanitizado()
        {
            await _sut.NotifyExtractionFailureAsync(new HttpRequestException("falló https://api.interna.com/v1?token=abc"), CancellationToken.None);

            _catalog.Verify(c => c.GetEtlAlertMessage(EtlErrorSanitizer.ConnectionFailedReason), Times.Once);
        }

        [Fact]
        public async Task NotifyExtractionFailureAsync_PayloadConSoloTituloMensajeFechaYCanales()
        {
            OutboxMessage message = await _sut.NotifyExtractionFailureAsync(new InvalidOperationException("Respuesta vacía del API externo."), CancellationToken.None);

            EtlOutboxPayload payload = JsonSerializer.Deserialize<EtlOutboxPayload>(message.Payload)!;
            payload.EventType.Should().Be("Alert");
            payload.Title.Should().Be("Alerta en proceso de ETL");
            payload.Message.Should().Be("Se generó un error 'Respuesta vacía del API externo.' en el proceso de ETL");
            payload.MessageDate.Should().NotBe(default(DateTime));
            payload.ClientId.Should().BeEmpty();
            payload.DocumentNumber.Should().BeEmpty();
            payload.Origen.Should().BeEmpty();
            payload.Destino.Should().BeEmpty();
            payload.Comentario.Should().BeEmpty();
            payload.FechaComentario.Should().Be(default(DateTime));
            payload.EstadoAnterior.Should().BeEmpty();
            payload.NuevoEstado.Should().BeEmpty();
            payload.NotificationChannel.Application.Should().BeTrue();
            payload.NotificationChannel.Email.Should().BeTrue();
            payload.NotificationChannel.TextMessages.Should().BeTrue();
        }

        [Fact]
        public async Task NotifyExtractionFailureAsync_ElJsonConservaTodosLosCamposDelContrato()
        {
            OutboxMessage message = await _sut.NotifyExtractionFailureAsync(new InvalidOperationException("x"), CancellationToken.None);

            using JsonDocument json = JsonDocument.Parse(message.Payload);
            json.RootElement.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(new[]
            {
                "ClientId", "EventType", "DocumentNumber", "Title", "Message", "MessageDate", "Origen", "Destino",
                "Comentario", "FechaComentario", "EstadoAnterior", "NuevoEstado", "NotificationChannel"
            });
            json.RootElement.GetProperty("NotificationChannel").EnumerateObject().Select(p => p.Name)
                .Should().BeEquivalentTo(new[] { "Application", "Email", "TextMessages" });
        }

        [Fact]
        public async Task NotifyExtractionFailureAsync_ReenviaElTokenAlRepositorio()
        {
            using var cts = new CancellationTokenSource();

            await _sut.NotifyExtractionFailureAsync(new InvalidOperationException("x"), cts.Token);

            _outbox.Verify(r => r.InsertBatchAsync(It.IsAny<IEnumerable<OutboxMessage>>(), cts.Token), Times.Once);
        }

        [Fact]
        public async Task NotifyExtractionFailureAsync_ErrorNulo_LanzaArgumentNullException()
        {
            Func<Task> act = () => _sut.NotifyExtractionFailureAsync(null!, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task NotifyExtractionFailureAsync_ErrorDelRepositorio_SePropaga()
        {
            _outbox.Setup(r => r.InsertBatchAsync(It.IsAny<IEnumerable<OutboxMessage>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("outbox caido"));

            Func<Task> act = () => _sut.NotifyExtractionFailureAsync(new InvalidOperationException("x"), CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("outbox caido");
        }

        [Fact]
        public void BuildOutboxMessage_TituloOMensajeNulos_QuedanVacios()
        {
            OutboxMessage message = EtlAlertNotifier.BuildOutboxMessage(null!, null!, new DateTime(2026, 10, 4, 8, 0, 0));

            EtlOutboxPayload payload = JsonSerializer.Deserialize<EtlOutboxPayload>(message.Payload)!;
            payload.Title.Should().BeEmpty();
            payload.Message.Should().BeEmpty();
        }
    }
}
