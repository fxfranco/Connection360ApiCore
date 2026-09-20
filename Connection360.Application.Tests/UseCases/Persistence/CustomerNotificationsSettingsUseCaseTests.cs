using Connection360.Application.DTOs;
using Connection360.Application.DTOs.Persistence;
using Connection360.Application.UseCases.Persistence;
using Connection360.Domain.Entities.Persistence;
using Connection360.Domain.Ports.Persistence;
using FluentAssertions;
using Moq;
using Xunit;

namespace Connection360.Application.Tests.UseCases.Persistence
{
    public class CustomerNotificationsSettingsUseCaseTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<ICustomerRepository> _customerRepositoryMock = new();
        private readonly Mock<ICustomerNotificationChannelsRepository> _channelsRepositoryMock = new();
        private readonly Mock<ICustomerNotificationEventRepository> _eventsRepositoryMock = new();
        private readonly CustomerNotificationsSettingsUseCase _sut;

        public CustomerNotificationsSettingsUseCaseTests()
        {
            _unitOfWorkMock.Setup(u => u.GetRepository<ICustomerRepository>()).Returns(_customerRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<ICustomerNotificationChannelsRepository>()).Returns(_channelsRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<ICustomerNotificationEventRepository>()).Returns(_eventsRepositoryMock.Object);
            _sut = new CustomerNotificationsSettingsUseCase(_unitOfWorkMock.Object);
        }

        [Fact]
        public async Task GetCustomerNotificationSettings_ConClienteInexistente_LanzaArgumentException()
        {
            _customerRepositoryMock.Setup(r => r.GetCustomerByIdAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync((Int64?)null);

            Func<Task> act = () => _sut.GetCustomerNotificationSettings("123", CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GetCustomerNotificationSettings_SinConfiguracionesPrevias_RetornaValoresPorDefectoEnFalse()
        {
            _customerRepositoryMock.Setup(r => r.GetCustomerByIdAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(5);
            _channelsRepositoryMock.Setup(r => r.GetByCustomerIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((CustomerNotificationChannels?)null);
            _eventsRepositoryMock.Setup(r => r.GetByCustomerIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((CustomerNotificationEvents?)null);

            CustomerNotificationsSettingsResponse result = await _sut.GetCustomerNotificationSettings("123", CancellationToken.None);

            result.NotificationChannels!.Application.Should().BeFalse();
            result.NotificationEvents!.ChangeState.Should().BeFalse();
            result.NotificationChannels.ClientId.Should().Be("123");
        }

        [Fact]
        public async Task GetCustomerNotificationSettings_ConConfiguracionesExistentes_MapeaLosValoresReales()
        {
            _customerRepositoryMock.Setup(r => r.GetCustomerByIdAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(5);
            _channelsRepositoryMock.Setup(r => r.GetByCustomerIdAsync(5, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CustomerNotificationChannels(1, 5, true, true, false));
            _eventsRepositoryMock.Setup(r => r.GetByCustomerIdAsync(5, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CustomerNotificationEvents(1, 5, true, false, true, false, true));

            CustomerNotificationsSettingsResponse result = await _sut.GetCustomerNotificationSettings("123", CancellationToken.None);

            result.NotificationChannels!.Application.Should().BeTrue();
            result.NotificationChannels.Email.Should().BeTrue();
            result.NotificationEvents!.ChangeState.Should().BeTrue();
            result.NotificationEvents.WithIssues.Should().BeTrue();
        }

        [Fact]
        public async Task CreateCustomerNotificationSettings_SinClientId_LanzaArgumentException()
        {
            var request = new CustomerNotificationsSettingsResponse
            {
                NotificationChannels = new NotificationChannelsResponse { ClientId = "" },
                NotificationEvents = new NotificationEventsResponse { ClientId = "" }
            };

            Func<Task> act = () => _sut.CreateCustomerNotificationSettings(request, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task CreateCustomerNotificationSettings_ConClienteInexistente_LanzaArgumentException()
        {
            var request = new CustomerNotificationsSettingsResponse
            {
                NotificationChannels = new NotificationChannelsResponse { ClientId = "123" },
                NotificationEvents = new NotificationEventsResponse { ClientId = "123" }
            };
            _customerRepositoryMock.Setup(r => r.GetCustomerByIdAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync((Int64?)null);

            Func<Task> act = () => _sut.CreateCustomerNotificationSettings(request, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task CreateCustomerNotificationSettings_ConDatosValidos_CreaCanalesYEventosYRetornaConIdsAsignados()
        {
            var request = new CustomerNotificationsSettingsResponse
            {
                NotificationChannels = new NotificationChannelsResponse { ClientId = "123", Application = true },
                NotificationEvents = new NotificationEventsResponse { ClientId = "123", ChangeState = true }
            };
            _customerRepositoryMock.Setup(r => r.GetCustomerByIdAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(5);
            _channelsRepositoryMock.Setup(r => r.CrearAsync(It.IsAny<CustomerNotificationChannels>(), It.IsAny<CancellationToken>())).ReturnsAsync(10);
            _eventsRepositoryMock.Setup(r => r.CrearAsync(It.IsAny<CustomerNotificationEvents>(), It.IsAny<CancellationToken>())).ReturnsAsync(20);

            CustomerNotificationsSettingsResponse result = await _sut.CreateCustomerNotificationSettings(request, CancellationToken.None);

            result.NotificationChannels!.NotificationChannelId.Should().Be(10);
            result.NotificationEvents!.NotificationEventId.Should().Be(20);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateCustomerNotificationSettings_SiCrearCanalesFalla_LanzaArgumentException()
        {
            var request = new CustomerNotificationsSettingsResponse
            {
                NotificationChannels = new NotificationChannelsResponse { ClientId = "123" },
                NotificationEvents = new NotificationEventsResponse { ClientId = "123" }
            };
            _customerRepositoryMock.Setup(r => r.GetCustomerByIdAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(5);
            _channelsRepositoryMock.Setup(r => r.CrearAsync(It.IsAny<CustomerNotificationChannels>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

            Func<Task> act = () => _sut.CreateCustomerNotificationSettings(request, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task UpdateCustomerNotificationSettings_ConDatosValidos_RetornaTrue()
        {
            var request = new CustomerNotificationsSettingsResponse
            {
                NotificationChannels = new NotificationChannelsResponse { ClientId = "123", NotificationChannelId = 1 },
                NotificationEvents = new NotificationEventsResponse { ClientId = "123", NotificationEventId = 2 }
            };
            _customerRepositoryMock.Setup(r => r.GetCustomerByIdAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(5);
            _channelsRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<CustomerNotificationChannels>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _eventsRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<CustomerNotificationEvents>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            Boolean result = await _sut.UpdateCustomerNotificationSettings(request, CancellationToken.None);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateCustomerNotificationSettings_SiActualizarCanalesFalla_LanzaArgumentException()
        {
            var request = new CustomerNotificationsSettingsResponse
            {
                NotificationChannels = new NotificationChannelsResponse { ClientId = "123", NotificationChannelId = 1 },
                NotificationEvents = new NotificationEventsResponse { ClientId = "123", NotificationEventId = 2 }
            };
            _customerRepositoryMock.Setup(r => r.GetCustomerByIdAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(5);
            _channelsRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<CustomerNotificationChannels>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

            Func<Task> act = () => _sut.UpdateCustomerNotificationSettings(request, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
        }
    }
}
