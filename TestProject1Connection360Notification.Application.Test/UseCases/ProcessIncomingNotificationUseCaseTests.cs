using Connection360Notification.Application.Services;
using Connection360Notification.Application.UseCases;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Ports.Outbound;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using DomainNotificationType = Connection360Notification.Domain.Enums.NotificationType;

namespace Connection360Notification.Application.Tests.UseCases
{
    public class ProcessIncomingNotificationUseCaseTests
    {
        private readonly Mock<INotificationRepository> _notificationRepositoryMock = new();
        private readonly Mock<INotificationDispatcher> _dispatcherMock = new();
        private readonly Mock<IBasicUserDataRepository> _basicUserDataRepositoryMock = new();
        private readonly Mock<INotificationRecipientContext> _recipientContextMock = new();
        private readonly ProcessIncomingNotificationUseCase _sut;

        public ProcessIncomingNotificationUseCaseTests()
        {
            _sut = new ProcessIncomingNotificationUseCase(
                _notificationRepositoryMock.Object,
                _dispatcherMock.Object,
                _basicUserDataRepositoryMock.Object,
                _recipientContextMock.Object,
                NullLogger<ProcessIncomingNotificationUseCase>.Instance);
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

            _dispatcherMock
                .Setup(n => n.DispatchAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
                .Callback(() => orden.Add("notify"))
                .Returns(Task.CompletedTask);

            await _sut.ExecuteAsync(notification, CancellationToken.None);

            orden.Should().Equal("save", "notify");
        }

        [Fact]
        public async Task ExecuteAsync_DespachaLaNotificacionCompletaPorLosCanalesDelUsuario()
        {
            var notification = CrearNotificacion(clientId: "456", title: "Cambio de estado");

            await _sut.ExecuteAsync(notification, CancellationToken.None);

            _dispatcherMock.Verify(n => n.DispatchAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
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

        [Fact]
        public async Task ExecuteAsync_SiPersistirFalla_PropagaLaExcepcionYNoNotifica()
        {
            var notification = CrearNotificacion();
            _notificationRepositoryMock
                .Setup(r => r.SaveAsync(notification, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("bd caida"));

            Func<Task> act = () => _sut.ExecuteAsync(notification, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("bd caida");
            _dispatcherMock.Verify(n => n.DispatchAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_SiNotificarFalla_PropagaLaExcepcionDespuesDePersistir()
        {
            var notification = CrearNotificacion();
            _dispatcherMock
                .Setup(n => n.DispatchAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("signalr"));

            Func<Task> act = () => _sut.ExecuteAsync(notification, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("signalr");
            _notificationRepositoryMock.Verify(r => r.SaveAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ConNotificacionNula_NoPersisteNiNotifica()
        {
            try { await _sut.ExecuteAsync(null!, CancellationToken.None); } catch (ArgumentNullException) { }

            _notificationRepositoryMock.Verify(r => r.SaveAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Never);
            _dispatcherMock.Verify(n => n.DispatchAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_PropagaElTokenDeCancelacionAlRepositorio()
        {
            using var cts = new CancellationTokenSource();
            var notification = CrearNotificacion();

            await _sut.ExecuteAsync(notification, cts.Token);

            _notificationRepositoryMock.Verify(r => r.SaveAsync(notification, cts.Token), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_PropagaElTokenDeCancelacionAlDespachador()
        {
            using var cts = new CancellationTokenSource();
            var notification = CrearNotificacion();

            await _sut.ExecuteAsync(notification, cts.Token);

            _dispatcherMock.Verify(d => d.DispatchAsync(notification, cts.Token), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ConsultaElUsuarioPorElClientIdComoDocument()
        {
            var notification = CrearNotificacion(clientId: "900123");

            await _sut.ExecuteAsync(notification, CancellationToken.None);

            _basicUserDataRepositoryMock.Verify(r => r.GetByDocumentAsync("900123", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_SiExisteElUsuario_LoDejaEnElContextoAntesDeDespachar()
        {
            var notification = CrearNotificacion(clientId: "900123");
            var usuario = new BasicUserData("auth0|1", "900123", "Ana Perez", "3001112233", "ana@correo.com");
            _basicUserDataRepositoryMock
                .Setup(r => r.GetByDocumentAsync("900123", It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuario);
            var orden = new List<String>();
            _recipientContextMock
                .Setup(c => c.Set(It.IsAny<BasicUserData?>()))
                .Callback<BasicUserData?>(u => orden.Add(u == usuario ? "context" : "context-otro"));
            _dispatcherMock
                .Setup(n => n.DispatchAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
                .Callback(() => orden.Add("notify"))
                .Returns(Task.CompletedTask);

            await _sut.ExecuteAsync(notification, CancellationToken.None);

            orden.Should().Equal("context", "notify");
        }

        [Fact]
        public async Task ExecuteAsync_SiNoExisteElUsuario_ElContextoQuedaEnNuloYSeNotificaIgual()
        {
            var notification = CrearNotificacion(clientId: "sin-registro");
            _basicUserDataRepositoryMock
                .Setup(r => r.GetByDocumentAsync("sin-registro", It.IsAny<CancellationToken>()))
                .ReturnsAsync((BasicUserData?)null);

            await _sut.ExecuteAsync(notification, CancellationToken.None);

            _recipientContextMock.Verify(c => c.Set(null), Times.Once);
            _dispatcherMock.Verify(n => n.DispatchAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_SiLaConsultaDelUsuarioFalla_NoImpideLaEntregaYElContextoQuedaEnNulo()
        {
            var notification = CrearNotificacion();
            _basicUserDataRepositoryMock
                .Setup(r => r.GetByDocumentAsync(It.IsAny<String>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("mongo caido"));

            Func<Task> act = () => _sut.ExecuteAsync(notification, CancellationToken.None);

            await act.Should().NotThrowAsync();
            _recipientContextMock.Verify(c => c.Set(null), Times.Once);
            _dispatcherMock.Verify(n => n.DispatchAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_SiLaConsultaDelUsuarioSeCancela_PropagaLaCancelacionYNoDespacha()
        {
            var notification = CrearNotificacion();
            _basicUserDataRepositoryMock
                .Setup(r => r.GetByDocumentAsync(It.IsAny<String>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            Func<Task> act = () => _sut.ExecuteAsync(notification, CancellationToken.None);

            await act.Should().ThrowAsync<OperationCanceledException>();
            _dispatcherMock.Verify(n => n.DispatchAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_PropagaElTokenDeCancelacionALaConsultaDelUsuario()
        {
            using var cts = new CancellationTokenSource();
            var notification = CrearNotificacion(clientId: "777");

            await _sut.ExecuteAsync(notification, cts.Token);

            _basicUserDataRepositoryMock.Verify(r => r.GetByDocumentAsync("777", cts.Token), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_SiPersistirFalla_NoConsultaElUsuario()
        {
            var notification = CrearNotificacion();
            _notificationRepositoryMock
                .Setup(r => r.SaveAsync(notification, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("bd caida"));

            try { await _sut.ExecuteAsync(notification, CancellationToken.None); } catch (InvalidOperationException) { }

            _basicUserDataRepositoryMock.Verify(r => r.GetByDocumentAsync(It.IsAny<String>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
