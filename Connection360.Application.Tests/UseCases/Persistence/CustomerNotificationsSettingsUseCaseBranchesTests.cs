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
    /// <summary>Ramas adicionales de CustomerNotificationsSettingsUseCase (transacciones, rollback y origen del ClientId).</summary>
    public class CustomerNotificationsSettingsUseCaseBranchesTests
    {
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<ICustomerRepository> _customers = new();
        private readonly Mock<ICustomerNotificationChannelsRepository> _channels = new();
        private readonly Mock<ICustomerNotificationEventRepository> _events = new();
        private readonly CustomerNotificationsSettingsUseCase _sut;

        public CustomerNotificationsSettingsUseCaseBranchesTests()
        {
            _uow.Setup(u => u.GetRepository<ICustomerRepository>()).Returns(_customers.Object);
            _uow.Setup(u => u.GetRepository<ICustomerNotificationChannelsRepository>()).Returns(_channels.Object);
            _uow.Setup(u => u.GetRepository<ICustomerNotificationEventRepository>()).Returns(_events.Object);
            _sut = new CustomerNotificationsSettingsUseCase(_uow.Object);
            _customers.Setup(r => r.GetCustomerByIdAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(5);
        }

        private static CustomerNotificationsSettingsResponse Settings(String? channelClient = "123", String? eventClient = "123") => new()
        {
            NotificationChannels = new NotificationChannelsResponse { ClientId = channelClient!, NotificationChannelId = 1, Application = true, Email = true, TextMessages = true },
            NotificationEvents = new NotificationEventsResponse
            {
                ClientId = eventClient!, NotificationEventId = 2, ChangeState = true, SuccessfulDelivery = true, WithIssues = true, ShipmentTransit = true, DeliveryReminder = true
            }
        };

        // ---------- Get ----------

        [Fact]
        public async Task GetCustomerNotificationSettings_SoloExistenCanales_UsaValoresPorDefectoParaEventos()
        {
            _channels.Setup(r => r.GetByCustomerIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new CustomerNotificationChannels(9, 5, true, false, true));
            _events.Setup(r => r.GetByCustomerIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((CustomerNotificationEvents?)null);

            CustomerNotificationsSettingsResponse result = await _sut.GetCustomerNotificationSettings("123");

            result.NotificationChannels!.NotificationChannelId.Should().Be(9);
            result.NotificationChannels.Application.Should().BeTrue();
            result.NotificationChannels.Email.Should().BeFalse();
            result.NotificationChannels.TextMessages.Should().BeTrue();
            result.NotificationEvents!.NotificationEventId.Should().Be(0);
            result.NotificationEvents.ClientId.Should().Be("123");
            result.NotificationEvents.ChangeState.Should().BeFalse();
            result.NotificationEvents.DeliveryReminder.Should().BeFalse();
        }

        [Fact]
        public async Task GetCustomerNotificationSettings_SoloExistenEventos_UsaValoresPorDefectoParaCanales()
        {
            _channels.Setup(r => r.GetByCustomerIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((CustomerNotificationChannels?)null);
            _events.Setup(r => r.GetByCustomerIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new CustomerNotificationEvents(4, 5, true, true, true, true, true));

            CustomerNotificationsSettingsResponse result = await _sut.GetCustomerNotificationSettings("123");

            result.NotificationChannels!.NotificationChannelId.Should().Be(0);
            result.NotificationChannels.Application.Should().BeFalse();
            result.NotificationEvents!.NotificationEventId.Should().Be(4);
            result.NotificationEvents.SuccessfulDelivery.Should().BeTrue();
            result.NotificationEvents.WithIssues.Should().BeTrue();
            result.NotificationEvents.ShipmentTransit.Should().BeTrue();
        }

        [Fact]
        public async Task GetCustomerNotificationSettings_ClienteInexistente_NoConsultaCanalesNiEventos()
        {
            _customers.Setup(r => r.GetCustomerByIdAsync("999", It.IsAny<CancellationToken>())).ReturnsAsync((Int64?)null);

            Func<Task> act = () => _sut.GetCustomerNotificationSettings("999");

            (await act.Should().ThrowAsync<ArgumentException>()).Which.ParamName.Should().Be("clientId");
            _channels.VerifyNoOtherCalls();
            _events.VerifyNoOtherCalls();
        }

        // ---------- Create ----------

        [Fact]
        public async Task CreateCustomerNotificationSettings_ClientIdSoloEnEventos_UsaElDeEventos()
        {
            var request = Settings(channelClient: null, eventClient: "123");
            _channels.Setup(r => r.CrearAsync(It.IsAny<CustomerNotificationChannels>(), It.IsAny<CancellationToken>())).ReturnsAsync(10);
            _events.Setup(r => r.CrearAsync(It.IsAny<CustomerNotificationEvents>(), It.IsAny<CancellationToken>())).ReturnsAsync(20);

            CustomerNotificationsSettingsResponse result = await _sut.CreateCustomerNotificationSettings(request);

            result.NotificationChannels!.NotificationChannelId.Should().Be(10);
            result.NotificationEvents!.NotificationEventId.Should().Be(20);
            _customers.Verify(r => r.GetCustomerByIdAsync("123", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateCustomerNotificationSettings_ClientIdVacioEnCanales_UsaElDeEventos()
        {
            var request = Settings(channelClient: "", eventClient: "123");
            _channels.Setup(r => r.CrearAsync(It.IsAny<CustomerNotificationChannels>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _events.Setup(r => r.CrearAsync(It.IsAny<CustomerNotificationEvents>(), It.IsAny<CancellationToken>())).ReturnsAsync(2);

            await _sut.CreateCustomerNotificationSettings(request);

            _customers.Verify(r => r.GetCustomerByIdAsync("123", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateCustomerNotificationSettings_SinClientIdEnNingunaSeccion_LanzaArgumentExceptionYHaceRollback()
        {
            var request = new CustomerNotificationsSettingsResponse();

            Func<Task> act = () => _sut.CreateCustomerNotificationSettings(request);

            (await act.Should().ThrowAsync<ArgumentException>()).Which.ParamName.Should().Be("clientId");
            _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateCustomerNotificationSettings_ClienteInexistente_NoIniciaTransaccion()
        {
            _customers.Setup(r => r.GetCustomerByIdAsync("404", It.IsAny<CancellationToken>())).ReturnsAsync((Int64?)null);

            Func<Task> act = () => _sut.CreateCustomerNotificationSettings(Settings("404", "404"));

            await act.Should().ThrowAsync<ArgumentException>();
            _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateCustomerNotificationSettings_SiCrearEventosFalla_LanzaArgumentExceptionYHaceRollback()
        {
            _channels.Setup(r => r.CrearAsync(It.IsAny<CustomerNotificationChannels>(), It.IsAny<CancellationToken>())).ReturnsAsync(10);
            _events.Setup(r => r.CrearAsync(It.IsAny<CustomerNotificationEvents>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

            Func<Task> act = () => _sut.CreateCustomerNotificationSettings(Settings());

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*eventos*");
            _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateCustomerNotificationSettings_SiCrearCanalesFalla_NoCreaEventosYHaceRollback()
        {
            _channels.Setup(r => r.CrearAsync(It.IsAny<CustomerNotificationChannels>(), It.IsAny<CancellationToken>())).ReturnsAsync(-1);

            Func<Task> act = () => _sut.CreateCustomerNotificationSettings(Settings());

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*canales*");
            _events.Verify(r => r.CrearAsync(It.IsAny<CustomerNotificationEvents>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateCustomerNotificationSettings_DebeMapearLosValoresALasEntidadesDeDominio()
        {
            CustomerNotificationChannels? capturedChannels = null;
            CustomerNotificationEvents? capturedEvents = null;
            _channels.Setup(r => r.CrearAsync(It.IsAny<CustomerNotificationChannels>(), It.IsAny<CancellationToken>()))
                .Callback<CustomerNotificationChannels, CancellationToken>((c, _) => capturedChannels = c).ReturnsAsync(1);
            _events.Setup(r => r.CrearAsync(It.IsAny<CustomerNotificationEvents>(), It.IsAny<CancellationToken>()))
                .Callback<CustomerNotificationEvents, CancellationToken>((e, _) => capturedEvents = e).ReturnsAsync(2);

            await _sut.CreateCustomerNotificationSettings(Settings());

            capturedChannels.Should().NotBeNull();
            capturedChannels!.IdNotificationChannel.Should().Be(0);
            capturedChannels.IdCustomer.Should().Be(5);
            capturedChannels.Application.Should().BeTrue();
            capturedChannels.Email.Should().BeTrue();
            capturedChannels.TextMessages.Should().BeTrue();
            capturedEvents.Should().NotBeNull();
            capturedEvents!.IdNotificationEvent.Should().Be(0);
            capturedEvents.IdCustomer.Should().Be(5);
            capturedEvents.ChangeState.Should().BeTrue();
            capturedEvents.SuccessfulDelivery.Should().BeTrue();
            capturedEvents.WithIssues.Should().BeTrue();
            capturedEvents.ShipmentTransit.Should().BeTrue();
            capturedEvents.DeliveryReminder.Should().BeTrue();
        }

        // ---------- Update ----------

        [Fact]
        public async Task UpdateCustomerNotificationSettings_SinClientId_LanzaArgumentExceptionYHaceRollback()
        {
            Func<Task> act = () => _sut.UpdateCustomerNotificationSettings(new CustomerNotificationsSettingsResponse());

            (await act.Should().ThrowAsync<ArgumentException>()).Which.ParamName.Should().Be("clientId");
            _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateCustomerNotificationSettings_ClienteInexistente_LanzaArgumentException()
        {
            _customers.Setup(r => r.GetCustomerByIdAsync("404", It.IsAny<CancellationToken>())).ReturnsAsync((Int64?)null);

            Func<Task> act = () => _sut.UpdateCustomerNotificationSettings(Settings("404", "404"));

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*cliente*");
            _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateCustomerNotificationSettings_ClientIdSoloEnEventos_UsaElDeEventos()
        {
            _channels.Setup(r => r.UpdateAsync(It.IsAny<CustomerNotificationChannels>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _events.Setup(r => r.UpdateAsync(It.IsAny<CustomerNotificationEvents>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            Boolean result = await _sut.UpdateCustomerNotificationSettings(Settings(channelClient: null, eventClient: "123"));

            result.Should().BeTrue();
            _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateCustomerNotificationSettings_SiActualizarEventosFalla_LanzaArgumentExceptionYHaceRollback()
        {
            _channels.Setup(r => r.UpdateAsync(It.IsAny<CustomerNotificationChannels>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _events.Setup(r => r.UpdateAsync(It.IsAny<CustomerNotificationEvents>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

            Func<Task> act = () => _sut.UpdateCustomerNotificationSettings(Settings());

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*eventos*");
            _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateCustomerNotificationSettings_SiActualizarCanalesFalla_NoActualizaEventosYHaceRollback()
        {
            _channels.Setup(r => r.UpdateAsync(It.IsAny<CustomerNotificationChannels>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

            Func<Task> act = () => _sut.UpdateCustomerNotificationSettings(Settings());

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*canales*");
            _events.Verify(r => r.UpdateAsync(It.IsAny<CustomerNotificationEvents>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateCustomerNotificationSettings_DebeMapearLosIdsYValoresALasEntidades()
        {
            CustomerNotificationChannels? capturedChannels = null;
            CustomerNotificationEvents? capturedEvents = null;
            _channels.Setup(r => r.UpdateAsync(It.IsAny<CustomerNotificationChannels>(), It.IsAny<CancellationToken>()))
                .Callback<CustomerNotificationChannels, CancellationToken>((c, _) => capturedChannels = c).ReturnsAsync(true);
            _events.Setup(r => r.UpdateAsync(It.IsAny<CustomerNotificationEvents>(), It.IsAny<CancellationToken>()))
                .Callback<CustomerNotificationEvents, CancellationToken>((e, _) => capturedEvents = e).ReturnsAsync(true);

            await _sut.UpdateCustomerNotificationSettings(Settings());

            capturedChannels!.IdNotificationChannel.Should().Be(1);
            capturedChannels.IdCustomer.Should().Be(5);
            capturedEvents!.IdNotificationEvent.Should().Be(2);
            capturedEvents.IdCustomer.Should().Be(5);
        }

        [Fact]
        public async Task UpdateCustomerNotificationSettings_SeccionDeCanalesNula_LanzaNullReferenceYHaceRollback()
        {
            var request = new CustomerNotificationsSettingsResponse
            {
                NotificationEvents = new NotificationEventsResponse { ClientId = "123" }
            };

            Func<Task> act = () => _sut.UpdateCustomerNotificationSettings(request);

            await act.Should().ThrowAsync<NullReferenceException>();
            _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
