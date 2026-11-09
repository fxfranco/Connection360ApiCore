using Connection360Notification.Application.DTOs;
using Connection360Notification.Application.UseCases;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Ports.Outbound;
using FluentAssertions;
using Moq;
using Xunit;
using AppNotificationStatus = Connection360Notification.Application.Enum.NotificationStatus;
using AppNotificationType = Connection360Notification.Application.Enum.NotificationType;
using DomainNotificationStatus = Connection360Notification.Domain.Enums.NotificationStatus;
using DomainNotificationType = Connection360Notification.Domain.Enums.NotificationType;

namespace Connection360Notification.Application.Tests.UseCases
{
    /// <summary>Pruebas complementarias de ramas de GetNotificationsUseCase.</summary>
    public class GetNotificationsUseCaseRamasTests
    {
        private static readonly DateTime Base = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private readonly Mock<INotificationRepository> _repoMock = new();
        private readonly GetNotificationsUseCase _sut;

        public GetNotificationsUseCaseRamasTests()
        {
            _sut = new GetNotificationsUseCase(_repoMock.Object);
        }

        private static NotificationMessage Crear(Int64 id, DateTime fecha, String cliente = "CLI-1")
        {
            var n = new NotificationMessage(cliente, DomainNotificationType.ChangeState, "Mensaje " + id, notificationDate: fecha);
            n.AssignSequentialId(id);
            return n;
        }

        [Fact]
        public async Task ExecuteGetNotificationsByClientAsync_ConCliente_ConsultaAlRepositorioYMapea()
        {
            var n = new NotificationMessage("CLI-1", DomainNotificationType.Comment, "Hola", "HBL-1", "Titulo",
                Base, DomainNotificationStatus.Read, Base.AddDays(1), 3, "ID-3");
            _repoMock.Setup(r => r.GetByClientAsync("CLI-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<NotificationMessage> { n });

            var result = await _sut.ExecuteGetNotificationsByClientAsync(new ClientSummaryRequest { IdClient = "CLI-1" }, CancellationToken.None);

            result.Should().ContainSingle();
            result[0].Id.Should().Be("ID-3");
            result[0].NotificationType.Should().Be(AppNotificationType.Comment);
            result[0].NotificationStatus.Should().Be(AppNotificationStatus.Read);
            result[0].DocumentNumber.Should().Be("HBL-1");
            _repoMock.Verify(r => r.GetByClientAsync("CLI-1", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteGetNotificationsByClientAsync_OrdenaPorFechaDescendente()
        {
            _repoMock.Setup(r => r.GetByClientAsync("CLI-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<NotificationMessage>
                {
                    Crear(1, Base), Crear(2, Base.AddDays(2)), Crear(3, Base.AddDays(1))
                });

            var result = await _sut.ExecuteGetNotificationsByClientAsync(new ClientSummaryRequest { IdClient = "CLI-1" }, CancellationToken.None);

            result.Select(r => r.IdNotification).Should().Equal(2, 3, 1);
        }

        [Fact]
        public async Task ExecuteGetNotificationsByClientAsync_ConRequestNulo_RetornaListaVacia()
        {
            var result = await _sut.ExecuteGetNotificationsByClientAsync(null!, CancellationToken.None);

            result.Should().BeEmpty();
            _repoMock.Verify(r => r.GetByClientAsync(It.IsAny<String>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ExecuteGetNotificationsByClientAsync_ConIdClientEnBlanco_RetornaListaVacia(String idClient)
        {
            var result = await _sut.ExecuteGetNotificationsByClientAsync(new ClientSummaryRequest { IdClient = idClient }, CancellationToken.None);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteGetNotificationsByClientAsync_SinResultados_RetornaListaVacia()
        {
            _repoMock.Setup(r => r.GetByClientAsync("CLI-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<NotificationMessage>());

            var result = await _sut.ExecuteGetNotificationsByClientAsync(new ClientSummaryRequest { IdClient = "CLI-1" }, CancellationToken.None);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteGetNotificationsByClientAsync_PropagaElTokenDeCancelacion()
        {
            using var cts = new CancellationTokenSource();
            _repoMock.Setup(r => r.GetByClientAsync("CLI-1", cts.Token)).ReturnsAsync(new List<NotificationMessage>());

            await _sut.ExecuteGetNotificationsByClientAsync(new ClientSummaryRequest { IdClient = "CLI-1" }, cts.Token);

            _repoMock.Verify(r => r.GetByClientAsync("CLI-1", cts.Token), Times.Once);
        }

        [Fact]
        public async Task ExecuteGetNotificationsByClientAsync_CuandoElRepositorioFalla_PropagaLaExcepcion()
        {
            _repoMock.Setup(r => r.GetByClientAsync(It.IsAny<String>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("bd"));

            Func<Task> act = () => _sut.ExecuteGetNotificationsByClientAsync(new ClientSummaryRequest { IdClient = "CLI-1" }, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task ExecuteGetNotificationsAllAsync_SinNotificaciones_RetornaListaVacia()
        {
            _repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<NotificationMessage>());

            var result = await _sut.ExecuteGetNotificationsAllAsync(CancellationToken.None);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteGetNotificationsAllAsync_IncluyeNotificacionesDeVariosClientes()
        {
            _repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<NotificationMessage> { Crear(1, Base, "A"), Crear(2, Base.AddDays(1), "B") });

            var result = await _sut.ExecuteGetNotificationsAllAsync(CancellationToken.None);

            result.Select(r => r.ClientId).Should().Equal("B", "A");
        }

        [Fact]
        public async Task ExecuteMarkAsReadAsync_ConRequestNulo_RetornaFalse()
        {
            var result = await _sut.ExecuteMarkAsReadAsync(null!, CancellationToken.None);

            result.Should().BeFalse();
            _repoMock.Verify(r => r.MarkAsReadAsync(It.IsAny<String>(), It.IsAny<Int64>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteMarkAsReadAsync_CuandoNoExiste_RetornaFalse()
        {
            _repoMock.Setup(r => r.MarkAsReadAsync("CLI-1", 9, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var result = await _sut.ExecuteMarkAsReadAsync(new NotificationsRequest { IdClient = "CLI-1", IdNotification = 9 }, CancellationToken.None);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExecuteMarkAsReadAsync_CuandoExiste_RetornaTrue()
        {
            _repoMock.Setup(r => r.MarkAsReadAsync("CLI-1", 9, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await _sut.ExecuteMarkAsReadAsync(new NotificationsRequest { IdClient = "CLI-1", IdNotification = 9 }, CancellationToken.None);

            result.Should().BeTrue();
        }
    }
}
