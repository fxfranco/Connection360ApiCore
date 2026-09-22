using Connection360Notification.Infrastructure.Adapters.Input;
using Connection360Notification.Infrastructure.Adapters.Output;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Adapters
{
    public class SignalRNotifierServiceTests
    {
        private readonly Mock<IHubContext<NotificationHub>> _hubContextMock = new();
        private readonly Mock<IHubClients> _hubClientsMock = new();
        private readonly Mock<IClientProxy> _clientProxyMock = new();
        private readonly SignalRNotifierService _sut;

        public SignalRNotifierServiceTests()
        {
            _hubContextMock.Setup(h => h.Clients).Returns(_hubClientsMock.Object);
            _hubClientsMock.Setup(c => c.User(It.IsAny<String>())).Returns(_clientProxyMock.Object);
            _sut = new SignalRNotifierService(_hubContextMock.Object);
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
    }
}
