using Connection360Notification.Application.Ports.Output;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Enums;
using Connection360Notification.Infrastructure.Adapters.Input;
using Connection360Notification.Infrastructure.Adapters.Output;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Adapters
{
    public class SignalRNotificationChannelTests
    {
        private readonly Mock<IHubContext<NotificationHub>> _hubContextMock = new();
        private readonly Mock<IHubClients> _hubClientsMock = new();
        private readonly Mock<IClientProxy> _clientProxyMock = new();
        private readonly SignalRNotificationChannel _sut;

        public SignalRNotificationChannelTests()
        {
            _hubContextMock.Setup(h => h.Clients).Returns(_hubClientsMock.Object);
            _hubClientsMock.Setup(c => c.User(It.IsAny<String>())).Returns(_clientProxyMock.Object);
            _sut = new SignalRNotificationChannel(_hubContextMock.Object);
        }

        [Fact]
        public async Task SendNotificationToUserAsync_EnviaSoloAlUsuarioEspecificado()
        {
            await _sut.SendNotificationToUserAsync("CLIENTE-123", "mensaje de prueba");

            _hubClientsMock.Verify(c => c.User("CLIENTE-123"), Times.Once);
        }

        [Fact]
        public async Task SendNotificationToUserAsync_EnviaElEventoReceiveNotification()
        {
            await _sut.SendNotificationToUserAsync("CLIENTE-123", "mensaje de prueba");

            _clientProxyMock.Verify(p => p.SendCoreAsync(
                "ReceiveNotification",
                It.IsAny<Object[]>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SendNotificationToUserAsync_IncluyeElMensajeYLosDatosEnElPayload()
        {
            Object[]? capturedArgs = null;
            _clientProxyMock
                .Setup(p => p.SendCoreAsync("ReceiveNotification", It.IsAny<Object[]>(), It.IsAny<CancellationToken>()))
                .Callback<String, Object[], CancellationToken>((_, args, _) => capturedArgs = args)
                .Returns(Task.CompletedTask);

            await _sut.SendNotificationToUserAsync("CLIENTE-123", "hola", new { Extra = "dato" });

            capturedArgs.Should().NotBeNull();
            capturedArgs![0].Should().NotBeNull();
        }

        [Fact]
        public async Task SendNotificationToUserAsync_ConDataNulo_NoLanzaExcepcion()
        {
            Func<Task> act = () => _sut.SendNotificationToUserAsync("CLIENTE-123", "mensaje", null);

            await act.Should().NotThrowAsync();
        }

        private async Task<Object> CapturarPayload(String mensaje, Object? data)
        {
            Object[]? capturedArgs = null;
            _clientProxyMock
                .Setup(p => p.SendCoreAsync("ReceiveNotification", It.IsAny<Object[]>(), It.IsAny<CancellationToken>()))
                .Callback<String, Object[], CancellationToken>((_, args, _) => capturedArgs = args)
                .Returns(Task.CompletedTask);

            await _sut.SendNotificationToUserAsync("CLIENTE-123", mensaje, data);

            capturedArgs.Should().ContainSingle();
            return capturedArgs![0];
        }

        [Fact]
        public async Task SendNotificationToUserAsync_ElPayloadExponeMessageDataYTimestamp()
        {
            var datos = new { Extra = "dato" };
            var antes = DateTime.UtcNow;

            var payload = await CapturarPayload("hola", datos);

            var tipo = payload.GetType();
            tipo.GetProperty("Message")!.GetValue(payload).Should().Be("hola");
            tipo.GetProperty("Data")!.GetValue(payload).Should().BeSameAs(datos);
            ((DateTime)tipo.GetProperty("Timestamp")!.GetValue(payload)!).Should().BeOnOrAfter(antes).And.BeOnOrBefore(DateTime.UtcNow);
        }

        [Fact]
        public async Task SendNotificationToUserAsync_SinData_ElPayloadTraeDataNulo()
        {
            var payload = await CapturarPayload("hola", null);

            payload.GetType().GetProperty("Data")!.GetValue(payload).Should().BeNull();
        }

        [Fact]
        public async Task SendNotificationToUserAsync_PasaElTokenPorDefectoAlProxy()
        {
            await _sut.SendNotificationToUserAsync("CLIENTE-123", "hola");

            _clientProxyMock.Verify(p => p.SendCoreAsync("ReceiveNotification", It.IsAny<Object[]>(), default), Times.Once);
        }

        [Fact]
        public async Task SendNotificationToUserAsync_SiElHubFalla_PropagaLaExcepcion()
        {
            _clientProxyMock
                .Setup(p => p.SendCoreAsync(It.IsAny<String>(), It.IsAny<Object[]>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("hub caido"));

            Func<Task> act = () => _sut.SendNotificationToUserAsync("CLIENTE-123", "hola");

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("hub caido");
        }

        // ---------- Contrato de canal (INotificationChannel) ----------

        [Fact]
        public void Channel_EsElCanalDeAplicacion()
        {
            _sut.Channel.Should().Be(NotificationChannelType.Application);
            ((INotificationChannel)_sut).Should().BeSameAs(_sut);
        }

        [Fact]
        public async Task SendAsync_EnviaAlClientIdLaNotificacionConElTituloComoMensajeYLaNotificacionCompletaComoData()
        {
            var notificacion = new NotificationMessage("CLIENTE-9", NotificationType.Comment, "contenido", title: "Titulo corto");
            Object[]? capturedArgs = null;
            _clientProxyMock
                .Setup(p => p.SendCoreAsync("ReceiveNotification", It.IsAny<Object[]>(), It.IsAny<CancellationToken>()))
                .Callback<String, Object[], CancellationToken>((_, args, _) => capturedArgs = args)
                .Returns(Task.CompletedTask);

            await _sut.SendAsync(notificacion, CancellationToken.None);

            _hubClientsMock.Verify(c => c.User("CLIENTE-9"), Times.Once);
            capturedArgs.Should().ContainSingle();
            var payload = capturedArgs![0];
            payload.GetType().GetProperty("Message")!.GetValue(payload).Should().Be("Titulo corto");
            payload.GetType().GetProperty("Data")!.GetValue(payload).Should().BeSameAs(notificacion);
        }

        [Fact]
        public async Task SendAsync_ReenviaElTokenDeCancelacionAlProxy()
        {
            using var cts = new CancellationTokenSource();
            var notificacion = new NotificationMessage("CLIENTE-9", NotificationType.Comment, "contenido");

            await _sut.SendAsync(notificacion, cts.Token);

            _clientProxyMock.Verify(p => p.SendCoreAsync("ReceiveNotification", It.IsAny<Object[]>(), cts.Token), Times.Once);
        }

        [Fact]
        public async Task SendAsync_ConNotificacionNula_LanzaArgumentNullException()
        {
            Func<Task> act = () => _sut.SendAsync(null!, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task SendAsync_SiElHubFalla_PropagaLaExcepcion()
        {
            _clientProxyMock
                .Setup(p => p.SendCoreAsync(It.IsAny<String>(), It.IsAny<Object[]>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("hub caido"));

            Func<Task> act = () => _sut.SendAsync(new NotificationMessage("C", NotificationType.Comment, "m"), CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("hub caido");
        }
    }
}
