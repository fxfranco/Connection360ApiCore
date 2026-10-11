using Connection360.Application.DTOs;
using Connection360.Application.DTOs.Persistence;
using Connection360.Application.UseCases.Persistence;
using Connection360.Domain.Entities.Persistence;
using Connection360.Domain.Enums;
using Connection360.Domain.Ports.Persistence;
using FluentAssertions;
using Moq;
using Xunit;

namespace Connection360.Application.Tests.UseCases.Persistence
{
    /// <summary>
    /// Rama de colaboradores (ADMIN, ANALISTAOPE, ANALISTASAC) del caso de uso: solo gestiona canales,
    /// usando ICollaboratorRepository para resolver el id y ICollaboratorNotificationChannelsRepository para persistir.
    /// </summary>
    public class CustomerNotificationsSettingsUseCaseColaboradoresTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<ICollaboratorRepository> _collaborators = new();
        private readonly Mock<ICollaboratorNotificationChannelsRepository> _channels = new();
        private readonly Mock<ICustomerRepository> _customers = new();
        private readonly Mock<ICustomerNotificationChannelsRepository> _customerChannels = new();
        private readonly Mock<ICustomerNotificationEventRepository> _customerEvents = new();
        private readonly CustomerNotificationsSettingsUseCase _sut;

        public static TheoryData<UserRoleApplication> RolesColaborador => new()
        {
            UserRoleApplication.ADMIN,
            UserRoleApplication.ANALISTAOPE,
            UserRoleApplication.ANALISTASAC
        };

        public CustomerNotificationsSettingsUseCaseColaboradoresTests()
        {
            _unitOfWork.Setup(u => u.GetRepository<ICollaboratorRepository>()).Returns(_collaborators.Object);
            _unitOfWork.Setup(u => u.GetRepository<ICollaboratorNotificationChannelsRepository>()).Returns(_channels.Object);
            _unitOfWork.Setup(u => u.GetRepository<ICustomerRepository>()).Returns(_customers.Object);
            _unitOfWork.Setup(u => u.GetRepository<ICustomerNotificationChannelsRepository>()).Returns(_customerChannels.Object);
            _unitOfWork.Setup(u => u.GetRepository<ICustomerNotificationEventRepository>()).Returns(_customerEvents.Object);
            _sut = new CustomerNotificationsSettingsUseCase(_unitOfWork.Object);
        }

        private static CustomerNotificationsSettingsResponse Request(String? clientId = "COL-1", Int64 channelId = 0)
            => new()
            {
                NotificationChannels = new NotificationChannelsResponse
                {
                    ClientId = clientId!,
                    NotificationChannelId = channelId,
                    Application = true,
                    Email = false,
                    TextMessages = true
                }
            };

        private void ColaboradorExiste(Int64? id = 7)
            => _collaborators.Setup(r => r.GetCollaboratorByIdAsync("COL-1", It.IsAny<CancellationToken>())).ReturnsAsync(id);

        private void NoSeTocaLaRamaDeClientes()
        {
            _customers.VerifyNoOtherCalls();
            _customerChannels.VerifyNoOtherCalls();
            _customerEvents.VerifyNoOtherCalls();
        }

        // ---------- Get ----------

        [Theory]
        [MemberData(nameof(RolesColaborador))]
        public async Task Get_ConRolColaboradorYRegistro_RetornaSoloCanalesMapeados(UserRoleApplication role)
        {
            ColaboradorExiste();
            _channels.Setup(r => r.GetByCollaboratorIdAsync(7, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CollaboratorNotificationChannels(3, 7, true, false, true));

            CustomerNotificationsSettingsResponse result = await _sut.GetCustomerNotificationSettings("COL-1", role);

            result.NotificationEvents.Should().BeNull();
            result.NotificationChannels.Should().BeEquivalentTo(new NotificationChannelsResponse
            {
                ClientId = "COL-1",
                NotificationChannelId = 3,
                Application = true,
                Email = false,
                TextMessages = true
            });
            NoSeTocaLaRamaDeClientes();
        }

        [Fact]
        public async Task Get_ConRolColaboradorSinRegistro_RetornaCanalesEnFalseConIdCero()
        {
            ColaboradorExiste();
            _channels.Setup(r => r.GetByCollaboratorIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync((CollaboratorNotificationChannels?)null);

            CustomerNotificationsSettingsResponse result = await _sut.GetCustomerNotificationSettings("COL-1", UserRoleApplication.ADMIN);

            result.NotificationChannels!.NotificationChannelId.Should().Be(0);
            result.NotificationChannels.Application.Should().BeFalse();
            result.NotificationChannels.Email.Should().BeFalse();
            result.NotificationChannels.TextMessages.Should().BeFalse();
            result.NotificationChannels.ClientId.Should().Be("COL-1");
            result.NotificationEvents.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData(0L)]
        public async Task Get_ConColaboradorInexistente_LanzaArgumentException(Int64? id)
        {
            ColaboradorExiste(id);

            Func<Task> act = () => _sut.GetCustomerNotificationSettings("COL-1", UserRoleApplication.ANALISTAOPE);

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*colaborador*");
            _channels.Verify(r => r.GetByCollaboratorIdAsync(It.IsAny<Int64>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // ---------- Create ----------

        [Theory]
        [MemberData(nameof(RolesColaborador))]
        public async Task Create_ConRolColaborador_CreaSoloCanalesAsignaElIdYHaceCommit(UserRoleApplication role)
        {
            ColaboradorExiste();
            CollaboratorNotificationChannels? creado = null;
            _channels.Setup(r => r.CrearAsync(It.IsAny<CollaboratorNotificationChannels>(), It.IsAny<CancellationToken>()))
                .Callback<CollaboratorNotificationChannels, CancellationToken>((c, _) => creado = c)
                .ReturnsAsync(55);
            CustomerNotificationsSettingsResponse request = Request();
            // Aunque lleguen eventos, para colaboradores se ignoran.
            request.NotificationEvents = new NotificationEventsResponse { ClientId = "COL-1", ChangeState = true };

            CustomerNotificationsSettingsResponse result = await _sut.CreateCustomerNotificationSettings(request, role);

            result.NotificationChannels!.NotificationChannelId.Should().Be(55);
            creado.Should().NotBeNull();
            creado!.IdCollaborator.Should().Be(7);
            creado.Application.Should().BeTrue();
            creado.Email.Should().BeFalse();
            creado.TextMessages.Should().BeTrue();
            _unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
            NoSeTocaLaRamaDeClientes();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task Create_ConRolColaboradorSinIdentificacion_LanzaArgumentExceptionYHaceRollback(String? clientId)
        {
            Func<Task> act = () => _sut.CreateCustomerNotificationSettings(Request(clientId), UserRoleApplication.ADMIN);

            await act.Should().ThrowAsync<ArgumentException>();
            _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Create_ConRolColaboradorSinCanales_LanzaArgumentException()
        {
            Func<Task> act = () => _sut.CreateCustomerNotificationSettings(new CustomerNotificationsSettingsResponse(), UserRoleApplication.ADMIN);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task Create_ConColaboradorInexistente_LanzaArgumentExceptionYNoAbreTransaccion()
        {
            ColaboradorExiste(null);

            Func<Task> act = () => _sut.CreateCustomerNotificationSettings(Request(), UserRoleApplication.ANALISTASAC);

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*colaborador*");
            _unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
            _channels.Verify(r => r.CrearAsync(It.IsAny<CollaboratorNotificationChannels>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Create_SiElRepositorioNoCreaElRegistro_LanzaArgumentExceptionYHaceRollback()
        {
            ColaboradorExiste();
            _channels.Setup(r => r.CrearAsync(It.IsAny<CollaboratorNotificationChannels>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

            Func<Task> act = () => _sut.CreateCustomerNotificationSettings(Request(), UserRoleApplication.ADMIN);

            await act.Should().ThrowAsync<ArgumentException>();
            _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ---------- Update ----------

        [Theory]
        [MemberData(nameof(RolesColaborador))]
        public async Task Update_ConRolColaborador_ActualizaSoloCanalesYHaceCommit(UserRoleApplication role)
        {
            ColaboradorExiste();
            CollaboratorNotificationChannels? actualizado = null;
            _channels.Setup(r => r.UpdateAsync(It.IsAny<CollaboratorNotificationChannels>(), It.IsAny<CancellationToken>()))
                .Callback<CollaboratorNotificationChannels, CancellationToken>((c, _) => actualizado = c)
                .ReturnsAsync(true);

            Boolean result = await _sut.UpdateCustomerNotificationSettings(Request(channelId: 9), role);

            result.Should().BeTrue();
            actualizado!.IdNotificationChannel.Should().Be(9);
            actualizado.IdCollaborator.Should().Be(7);
            actualizado.Application.Should().BeTrue();
            actualizado.TextMessages.Should().BeTrue();
            _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
            NoSeTocaLaRamaDeClientes();
        }

        [Fact]
        public async Task Update_ConRolColaboradorSinIdentificacion_LanzaArgumentException()
        {
            Func<Task> act = () => _sut.UpdateCustomerNotificationSettings(new CustomerNotificationsSettingsResponse(), UserRoleApplication.ADMIN);

            await act.Should().ThrowAsync<ArgumentException>();
            _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Update_ConColaboradorInexistente_LanzaArgumentException()
        {
            ColaboradorExiste(0);

            Func<Task> act = () => _sut.UpdateCustomerNotificationSettings(Request(channelId: 9), UserRoleApplication.ANALISTAOPE);

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*colaborador*");
            _channels.Verify(r => r.UpdateAsync(It.IsAny<CollaboratorNotificationChannels>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Update_SiElRegistroNoSeActualiza_LanzaArgumentExceptionYHaceRollback()
        {
            ColaboradorExiste();
            _channels.Setup(r => r.UpdateAsync(It.IsAny<CollaboratorNotificationChannels>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

            Func<Task> act = () => _sut.UpdateCustomerNotificationSettings(Request(channelId: 9), UserRoleApplication.ADMIN);

            await act.Should().ThrowAsync<ArgumentException>();
            _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ---------- Rol CLIENT y roles no soportados ----------

        [Fact]
        public async Task Get_ConRolClient_NoUsaLosRepositoriosDeColaborador()
        {
            _customers.Setup(r => r.GetCustomerByIdAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(5);
            _customerChannels.Setup(r => r.GetByCustomerIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new CustomerNotificationChannels(1, 5, true, true, false));
            _customerEvents.Setup(r => r.GetByCustomerIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new CustomerNotificationEvents(1, 5, true, false, true, false, true));

            CustomerNotificationsSettingsResponse result = await _sut.GetCustomerNotificationSettings("123", UserRoleApplication.CLIENT);

            result.NotificationChannels.Should().NotBeNull();
            result.NotificationEvents.Should().NotBeNull();
            _collaborators.VerifyNoOtherCalls();
            _channels.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Create_ConRolClient_NoUsaLosRepositoriosDeColaborador()
        {
            _customers.Setup(r => r.GetCustomerByIdAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(5);
            _customerChannels.Setup(r => r.CrearAsync(It.IsAny<CustomerNotificationChannels>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _customerEvents.Setup(r => r.CrearAsync(It.IsAny<CustomerNotificationEvents>(), It.IsAny<CancellationToken>())).ReturnsAsync(2);
            var request = new CustomerNotificationsSettingsResponse
            {
                NotificationChannels = new NotificationChannelsResponse { ClientId = "123" },
                NotificationEvents = new NotificationEventsResponse { ClientId = "123" }
            };

            await _sut.CreateCustomerNotificationSettings(request, UserRoleApplication.CLIENT);

            _collaborators.VerifyNoOtherCalls();
            _channels.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Update_ConRolClient_NoUsaLosRepositoriosDeColaborador()
        {
            _customers.Setup(r => r.GetCustomerByIdAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(5);
            _customerChannels.Setup(r => r.UpdateAsync(It.IsAny<CustomerNotificationChannels>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _customerEvents.Setup(r => r.UpdateAsync(It.IsAny<CustomerNotificationEvents>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            var request = new CustomerNotificationsSettingsResponse
            {
                NotificationChannels = new NotificationChannelsResponse { ClientId = "123", NotificationChannelId = 1 },
                NotificationEvents = new NotificationEventsResponse { ClientId = "123", NotificationEventId = 2 }
            };

            Boolean result = await _sut.UpdateCustomerNotificationSettings(request, UserRoleApplication.CLIENT);

            result.Should().BeTrue();
            _collaborators.VerifyNoOtherCalls();
            _channels.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Get_ConRolNoAsignado_LanzaArgumentExceptionSinConsultarRepositorios()
        {
            Func<Task> act = () => _sut.GetCustomerNotificationSettings("123", UserRoleApplication.UNASSIGNED);

            await act.Should().ThrowAsync<ArgumentException>().WithParameterName("role");
            _unitOfWork.Verify(u => u.GetRepository<It.IsAnyType>(), Times.Never);
        }

        [Fact]
        public async Task CreateYUpdate_ConRolNoAsignado_LanzanArgumentException()
        {
            Func<Task> create = () => _sut.CreateCustomerNotificationSettings(Request(), UserRoleApplication.UNASSIGNED);
            Func<Task> update = () => _sut.UpdateCustomerNotificationSettings(Request(), UserRoleApplication.UNASSIGNED);

            await create.Should().ThrowAsync<ArgumentException>().WithParameterName("role");
            await update.Should().ThrowAsync<ArgumentException>().WithParameterName("role");
            _unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
