using Connection360Notification.Application.DTOs;
using Connection360Notification.Application.UseCases;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Ports.Outbound;
using FluentAssertions;
using Moq;
using Xunit;
using DomainNotificationType = Connection360Notification.Domain.Enums.NotificationType;

namespace Connection360Notification.Application.Tests.UseCases
{
    public class GetNotificationsUseCaseTests
    {
        private readonly Mock<INotificationRepository> _notificationRepositoryMock = new();
        private readonly GetNotificationsUseCase _sut;

        public GetNotificationsUseCaseTests()
        {
            _sut = new GetNotificationsUseCase(_notificationRepositoryMock.Object);
        }

        private static NotificationMessage CrearNotificacion(String clientId = "123", Int64 idNotification = 1, DateTime? notificationDate = null)
        {
            var notification = new NotificationMessage(clientId, DomainNotificationType.Comment, "Mensaje de prueba", notificationDate: notificationDate ?? default);
            notification.AssignSequentialId(idNotification);
            return notification;
        }

        [Fact]
        public async Task ExecuteGetNotificationsAllAsync_RetornaLasNotificacionesDelClienteMapeadas()
        {
            var request = new ClientSummaryRequest { IdClient = "123" };

            _notificationRepositoryMock
                .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<NotificationMessage> { CrearNotificacion() });

            var result = await _sut.ExecuteGetNotificationsAllAsync(CancellationToken.None);

            result.Should().NotBeNull();
            result.Should().ContainSingle();
            result[0].IdNotification.Should().Be(1);
        }

        [Fact]
        public async Task ExecuteGetNotificationsAllAsync_TodosLosElementosTienenIdPositivo()
        {
            var request = new ClientSummaryRequest { IdClient = "123" };

            _notificationRepositoryMock
                .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<NotificationMessage> { CrearNotificacion(idNotification: 1), CrearNotificacion(idNotification: 2) });

            var result = await _sut.ExecuteGetNotificationsAllAsync(CancellationToken.None);

            result.Should().OnlyContain(n => n.IdNotification > 0);
        }

        [Fact]
        public async Task ExecuteGetNotificationsByClientAsync_SinIdClient_RetornaListaVaciaSinConsultarElRepositorio()
        {
            var request = new ClientSummaryRequest { IdClient = String.Empty };

            var result = await _sut.ExecuteGetNotificationsByClientAsync(request, CancellationToken.None);

            result.Should().BeEmpty();
            _notificationRepositoryMock.Verify(r => r.GetByClientAsync(It.IsAny<String>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteGetNotificationsAllAsync_OrdenaPorFechaDeNotificacionDescendente()
        {
            var request = new ClientSummaryRequest { IdClient = "123" };
            var antigua = CrearNotificacion(idNotification: 1, notificationDate: DateTime.UtcNow.AddDays(-1));
            var reciente = CrearNotificacion(idNotification: 2, notificationDate: DateTime.UtcNow);

            _notificationRepositoryMock
                .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<NotificationMessage> { antigua, reciente });

            var result = await _sut.ExecuteGetNotificationsAllAsync(CancellationToken.None);

            result.Should().HaveCount(2);
            result[0].IdNotification.Should().Be(2);
            result[1].IdNotification.Should().Be(1);
        }

        [Fact]
        public async Task ExecuteMarkAsReadAsync_DelegaEnElRepositorioYRetornaElResultado()
        {
            var request = new NotificationsRequest { IdClient = "123", IdNotification = 1 };

            _notificationRepositoryMock
                .Setup(r => r.MarkAsReadAsync("123", 1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _sut.ExecuteMarkAsReadAsync(request, CancellationToken.None);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteMarkAsReadAsync_SinIdClient_RetornaFalseSinConsultarElRepositorio()
        {
            var request = new NotificationsRequest { IdClient = String.Empty, IdNotification = 1 };

            var result = await _sut.ExecuteMarkAsReadAsync(request, CancellationToken.None);

            result.Should().BeFalse();
            _notificationRepositoryMock.Verify(r => r.MarkAsReadAsync(It.IsAny<String>(), It.IsAny<Int64>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
