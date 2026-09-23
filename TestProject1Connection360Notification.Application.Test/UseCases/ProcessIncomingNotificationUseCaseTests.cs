using Connection360Notification.Application.Ports.Output;
using Connection360Notification.Application.UseCases;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Ports.Outbound;
using FluentAssertions;
using Moq;
using Xunit;
using DomainNotificationType = Connection360Notification.Domain.Enums.NotificationType;

namespace Connection360Notification.Application.Tests.UseCases
{
    public class ProcessIncomingNotificationUseCaseTests
    {
        private readonly Mock<INotificationRepository> _notificationRepositoryMock = new();
        private readonly Mock<INotifierService> _notifierServiceMock = new();
        private readonly ProcessIncomingNotificationUseCase _sut;

        public ProcessIncomingNotificationUseCaseTests()
        {
            _sut = new ProcessIncomingNotificationUseCase(_notificationRepositoryMock.Object, _notifierServiceMock.Object);
        }

        private static NotificationMessage CrearNotificacion(String clientId = "123", String title = "Titulo de prueba")
        {
            return new NotificationMessage(clientId, DomainNotificationType.Comment, "Mensaje de prueba", title: title);
        }

        [Fact]
        public async Task ExecuteAsync_PrimeroPersisteYLuegoNotifica()
        {
            var notification = CrearNotificacion();
            var orden = new List<String>();

            _notificationRepositoryMock
                .Setup(r => r.SaveAsync(notification, It.IsAny<CancellationToken>()))
                .Callback(() => orden.Add("save"))
                .Returns(Task.CompletedTask);

            _notifierServiceMock
                .Setup(n => n.SendNotificationToUserAsync(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<Object>()))
                .Callback(() => orden.Add("notify"))
                .Returns(Task.CompletedTask);

            await _sut.ExecuteAsync(notification, CancellationToken.None);

            orden.Should().Equal("save", "notify");
        }

        [Fact]
        public async Task ExecuteAsync_NotificaAlClientIdConElTituloYLaNotificacionCompletaComoData()
        {
            var notification = CrearNotificacion(clientId: "456", title: "Cambio de estado");

            await _sut.ExecuteAsync(notification, CancellationToken.None);

            _notifierServiceMock.Verify(n => n.SendNotificationToUserAsync("456", "Cambio de estado", notification), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_PersisteLaNotificacionRecibida()
        {
            var notification = CrearNotificacion();

            await _sut.ExecuteAsync(notification, CancellationToken.None);

            _notificationRepositoryMock.Verify(r => r.SaveAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ConNotificacionNula_LanzaArgumentNullException()
        {
            Func<Task> act = () => _sut.ExecuteAsync(null!, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }
    }
}
