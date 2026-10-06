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
    /// <summary>Casos complementarios de los casos de uso de persistencia (ramas no cubiertas por las pruebas por clase).</summary>
    public class PersistenceUseCasesComplementTests
    {
        private readonly Mock<IUnitOfWork> _uow = new();

        // ---------- CustomerNotificationEventsUseCase ----------

        [Fact]
        public async Task Events_GetByIdAsync_CuandoExiste_MapeaTodosLosCampos()
        {
            var repo = new Mock<ICustomerNotificationEventRepository>();
            _uow.Setup(u => u.GetRepository<ICustomerNotificationEventRepository>()).Returns(repo.Object);
            repo.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(new CustomerNotificationEvents(3, 8, true, false, true, false, true));

            CustomerNotificationEventsResponseDto? result = await new CustomerNotificationEventsUseCase(_uow.Object).GetByIdAsync(3);

            result.Should().Be(new CustomerNotificationEventsResponseDto(3, 8, true, false, true, false, true));
        }

        [Fact]
        public async Task Events_ListAllAsync_SinRegistros_RetornaColeccionVacia()
        {
            var repo = new Mock<ICustomerNotificationEventRepository>();
            _uow.Setup(u => u.GetRepository<ICustomerNotificationEventRepository>()).Returns(repo.Object);
            repo.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<CustomerNotificationEvents>());

            IEnumerable<CustomerNotificationEventsResponseDto> result = await new CustomerNotificationEventsUseCase(_uow.Object).ListAllAsync();

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task Events_CrearAsync_ConClienteInvalido_HaceRollbackYLanzaArgumentException()
        {
            var repo = new Mock<ICustomerNotificationEventRepository>();
            _uow.Setup(u => u.GetRepository<ICustomerNotificationEventRepository>()).Returns(repo.Object);

            // La entidad de dominio rechaza IdCustomer <= 0 antes de abrir la transaccion.
            Func<Task> act = () => new CustomerNotificationEventsUseCase(_uow.Object)
                .CrearAsync(new CreateCustomerNotificationEventsDto(0, true, true, true, true, true));

            await act.Should().ThrowAsync<ArgumentException>();
            repo.Verify(r => r.CrearAsync(It.IsAny<CustomerNotificationEvents>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ---------- CustomerNotificationChannelsUseCase ----------

        [Fact]
        public async Task Channels_ListAllAsync_SinRegistros_RetornaColeccionVacia()
        {
            var repo = new Mock<ICustomerNotificationChannelsRepository>();
            _uow.Setup(u => u.GetRepository<ICustomerNotificationChannelsRepository>()).Returns(repo.Object);
            repo.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<CustomerNotificationChannels>());

            IEnumerable<CustomerNotificationChannelsResponseDto> result = await new CustomerNotificationChannelsUseCase(_uow.Object).ListAllAsync();

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task Channels_CrearAsync_ConClienteInvalido_LanzaArgumentExceptionSinAbrirTransaccion()
        {
            var repo = new Mock<ICustomerNotificationChannelsRepository>();
            _uow.Setup(u => u.GetRepository<ICustomerNotificationChannelsRepository>()).Returns(repo.Object);

            Func<Task> act = () => new CustomerNotificationChannelsUseCase(_uow.Object)
                .CrearAsync(new CreateCustomerNotificationChannelsDto(0, true, true, true));

            await act.Should().ThrowAsync<ArgumentException>();
            _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ---------- MasterSettingsUseCase ----------

        [Fact]
        public async Task MasterSettings_GetAsync_ConCamposDeTextoNulos_RetornaCadenasVacias()
        {
            var repo = new Mock<IMasterSettingsRepository>();
            _uow.Setup(u => u.GetRepository<IMasterSettingsRepository>()).Returns(repo.Object);
            // El constructor sin parametros deja CurrencyType/Language/TimeZone en null.
            repo.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new MasterSettings());

            MasterSettingsResponse result = await new MasterSettingsUseCase(_uow.Object).GetAsync();

            result.Location.CurrencyType.Should().BeEmpty();
            result.Location.Language.Should().BeEmpty();
            result.System.TimeZone.Should().BeEmpty();
            result.System.DataRetentionDays.Should().Be(0);
        }

        [Fact]
        public async Task MasterSettings_UpdateAsync_Fallida_NoDevuelveDtoYHaceRollback()
        {
            var repo = new Mock<IMasterSettingsRepository>();
            _uow.Setup(u => u.GetRepository<IMasterSettingsRepository>()).Returns(repo.Object);
            repo.Setup(r => r.UpdateAsync(It.IsAny<MasterSettings>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

            Func<Task> act = () => new MasterSettingsUseCase(_uow.Object)
                .UpdateAsync(new MasterSettingsResponseDto(1, true, true, true, "COP", "es", "UTC-5", 30));

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*actualizaci*");
            _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task MasterSettings_CreateAsync_DebePropagarElCancellationToken()
        {
            var repo = new Mock<IMasterSettingsRepository>();
            _uow.Setup(u => u.GetRepository<IMasterSettingsRepository>()).Returns(repo.Object);
            using var cts = new CancellationTokenSource();
            repo.Setup(r => r.CrearAsync(It.IsAny<MasterSettings>(), cts.Token)).ReturnsAsync(77);

            MasterSettingsResponseDto result = await new MasterSettingsUseCase(_uow.Object)
                .CreateAsync(new CreateMasterSettingsDto(true, false, true, "COP", "es", "UTC-5", 30), cts.Token);

            result.IdMasterSettings.Should().Be(77);
            result.CurrencyType.Should().Be("COP");
            result.DataRetentionDays.Should().Be(30);
            _uow.Verify(u => u.BeginTransactionAsync(cts.Token), Times.Once);
            _uow.Verify(u => u.CommitAsync(cts.Token), Times.Once);
        }

        // ---------- CollaboratorUseCase ----------

        [Fact]
        public async Task Collaborator_CreateCustomerCollaboratorAsync_ClienteInexistente_NoConsultaAlColaborador()
        {
            var customers = new Mock<ICustomerRepository>();
            var collaborators = new Mock<ICollaboratorRepository>(MockBehavior.Strict);
            _uow.Setup(u => u.GetRepository<ICustomerRepository>()).Returns(customers.Object);
            _uow.Setup(u => u.GetRepository<ICollaboratorRepository>()).Returns(collaborators.Object);
            customers.Setup(r => r.GetCustomerByIdAsync("C", It.IsAny<CancellationToken>())).ReturnsAsync((Int64?)null);

            Func<Task> act = () => new CollaboratorUseCase(_uow.Object).CreateCustomerCollaboratorAsync("C", "COL");

            (await act.Should().ThrowAsync<ArgumentException>()).Which.ParamName.Should().Be("customerId");
            collaborators.VerifyNoOtherCalls();
            _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Collaborator_CreateCustomerCollaboratorAsync_ColaboradorInexistente_IndicaElParametro()
        {
            var customers = new Mock<ICustomerRepository>();
            var collaborators = new Mock<ICollaboratorRepository>();
            _uow.Setup(u => u.GetRepository<ICustomerRepository>()).Returns(customers.Object);
            _uow.Setup(u => u.GetRepository<ICollaboratorRepository>()).Returns(collaborators.Object);
            customers.Setup(r => r.GetCustomerByIdAsync("C", It.IsAny<CancellationToken>())).ReturnsAsync(4);
            collaborators.Setup(r => r.GetCollaboratorByIdAsync("COL", It.IsAny<CancellationToken>())).ReturnsAsync((Int64?)null);

            Func<Task> act = () => new CollaboratorUseCase(_uow.Object).CreateCustomerCollaboratorAsync("C", "COL");

            (await act.Should().ThrowAsync<ArgumentException>()).Which.ParamName.Should().Be("collaboratorId");
        }
    }
}
