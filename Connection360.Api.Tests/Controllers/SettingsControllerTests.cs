using Connection360.Domain.Enums;
using Connection360.Api.Controllers;
using Connection360.Api.Models;
using Connection360.Api.Tests.TestSupport;
using Connection360.Application.DTOs;
using Connection360.Application.DTOs.Persistence;
using Connection360.Application.Ports;
using Connection360.Application.Ports.Persistence;
using Connection360.Domain.Dtos;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Connection360.Api.Tests.Controllers
{
    public class SettingsControllerTests
    {
        private readonly Mock<IGetUserManagementUseCase> _userManagementMock = new();
        private readonly Mock<ICustomerUseCase> _customerUseCaseMock = new();
        private readonly Mock<ICustomerNotificationsSettingsUseCase> _notificationsSettingsMock = new();
        private readonly Mock<IMasterSettingsUseCase> _masterSettingsMock = new();
        private readonly Mock<ICollaboratorUseCase> _collaboratorUseCaseMock = new();
        private readonly Mock<IOutboxMessagesUseCase> _outboxMessagesUseCase = new();
        private readonly SettingsController _sut;

        public SettingsControllerTests()
        {
            _sut = new SettingsController(_userManagementMock.Object, _customerUseCaseMock.Object, _notificationsSettingsMock.Object, _masterSettingsMock.Object, _collaboratorUseCaseMock.Object, _outboxMessagesUseCase.Object)
            {
                // El controlador consulta User.IsInRole(...): sin un HttpContext con rol lanzaria NullReferenceException.
                ControllerContext = ControllerContextFactory.Create("ADMIN")
            };
        }

        [Fact]
        public async Task GetNotificationsSettings_RetornaOkConElResultado()
        {
            var expected = new CustomerNotificationsSettingsResponse();
            _notificationsSettingsMock.Setup(u => u.GetCustomerNotificationSettings("123", It.IsAny<UserRoleApplication>(), It.IsAny<CancellationToken>())).ReturnsAsync(expected);

            IActionResult result = await _sut.GetNotificationsSettings("123", CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(expected);
        }

        [Fact]
        public async Task CreateNotificationsSettings_RetornaCreatedAtRouteConElResultado()
        {
            var expected = new CustomerNotificationsSettingsResponse
            {
                NotificationChannels = new NotificationChannelsResponse { NotificationChannelId = 1 },
                NotificationEvents = new NotificationEventsResponse { NotificationEventId = 2 }
            };
            _notificationsSettingsMock.Setup(u => u.CreateCustomerNotificationSettings(It.IsAny<CustomerNotificationsSettingsResponse>(), It.IsAny<UserRoleApplication>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            IActionResult result = await _sut.CreateNotificationsSettings(new CustomerNotificationsSettingsResponse(), CancellationToken.None);

            var created = result.Should().BeOfType<CreatedAtRouteResult>().Subject;
            created.RouteName.Should().Be(nameof(SettingsController.CreateNotificationsSettings));
            created.Value.Should().BeSameAs(expected);
        }

        [Fact]
        public async Task UpdateNotificationsSettings_RetornaNoContent()
        {
            _notificationsSettingsMock.Setup(u => u.UpdateCustomerNotificationSettings(It.IsAny<CustomerNotificationsSettingsResponse>(), It.IsAny<UserRoleApplication>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            IActionResult result = await _sut.UpdateNotificationsSettings(new CustomerNotificationsSettingsResponse(), CancellationToken.None);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task GetMasterSettings_RetornaOkConElResultado()
        {
            var expected = new MasterSettingsResponse { IdMasterSettings = 1 };
            _masterSettingsMock.Setup(u => u.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);

            IActionResult result = await _sut.GetMasterSettings(CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(expected);
        }

        [Fact]
        public async Task CreateMasterSettings_RetornaCreatedAtRoute()
        {
            var dto = new CreateMasterSettingsDto(true, false, true, "COP", "es", "America/Bogota", 30);
            var expected = new MasterSettingsResponseDto(1, true, false, true, "COP", "es", "America/Bogota", 30);
            _masterSettingsMock.Setup(u => u.CreateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

            IActionResult result = await _sut.CreateMasterSettings(dto, CancellationToken.None);

            var created = result.Should().BeOfType<CreatedAtRouteResult>().Subject;
            created.RouteName.Should().Be(nameof(SettingsController.CreateMasterSettings));
        }

        [Fact]
        public async Task UpdateMasterSettings_RetornaNoContent()
        {
            var dto = new MasterSettingsResponseDto(1, true, false, true, "COP", "es", "America/Bogota", 30);
            _masterSettingsMock.Setup(u => u.UpdateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(dto);

            IActionResult result = await _sut.UpdateMasterSettings(dto, CancellationToken.None);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task GetUsersListSettings_ConPaginaMayorACero_LaDecrementaAntesDeConsultar()
        {
            _userManagementMock.Setup(u => u.GetAllUsersAsync(It.IsAny<UsersManagementRequest>())).ReturnsAsync(new List<Auth0UserDto>());

            await _sut.GetUsersListSettings(3, 10, CancellationToken.None);

            _userManagementMock.Verify(u => u.GetAllUsersAsync(It.Is<UsersManagementRequest>(r => r.Page == 2)), Times.Once);
        }

        [Fact]
        public async Task GetUsersListSettings_ConResultadoValido_RetornaOkConPagedResult()
        {
            var users = new List<Auth0UserDto> { new() { UserId = "u1" } };
            _userManagementMock.Setup(u => u.GetAllUsersAsync(It.IsAny<UsersManagementRequest>())).ReturnsAsync(users);

            IActionResult result = await _sut.GetUsersListSettings(1, 10, CancellationToken.None);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeOfType<PagedResult<Object>>();
        }

        [Fact]
        public async Task GetUsersListSettings_SiElUseCaseLanzaExcepcion_RetornaProblemDetails500()
        {
            _userManagementMock.Setup(u => u.GetAllUsersAsync(It.IsAny<UsersManagementRequest>())).ThrowsAsync(new InvalidOperationException("fallo Auth0"));

            IActionResult result = await _sut.GetUsersListSettings(1, 10, CancellationToken.None);

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetUsersByIdSettings_ConUsuarioEncontrado_RetornaOk()
        {
            var expected = new Auth0UserDto { UserId = "u1" };
            _userManagementMock.Setup(u => u.GetUsersByIdAsync("u1")).ReturnsAsync(expected);

            IActionResult result = await _sut.GetUsersByIdSettings("u1", CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(expected);
        }

        [Fact]
        public async Task GetUsersByIdSettings_ConservaLosDatosDeUserMetadataYElRol()
        {
            var expected = new Auth0UserDto
            {
                UserId = "u1", Company = "Acme", Document = "900123", FullName = "Ana Perez", Phone = "+573113232323", RoleId = "rol_1", RoleName = "ADMIN"
            };
            _userManagementMock.Setup(u => u.GetUsersByIdAsync("u1")).ReturnsAsync(expected);

            IActionResult result = await _sut.GetUsersByIdSettings("u1", CancellationToken.None);

            var dto = result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<Auth0UserDto>().Subject;
            dto.Company.Should().Be("Acme");
            dto.Document.Should().Be("900123");
            dto.FullName.Should().Be("Ana Perez");
            dto.Phone.Should().Be("+573113232323");
            dto.RoleName.Should().Be("ADMIN");
        }

        [Fact]
        public async Task GetUsersByIdSettings_SiLanzaExcepcion_RetornaProblemDetails500()
        {
            _userManagementMock.Setup(u => u.GetUsersByIdAsync("u1")).ThrowsAsync(new InvalidOperationException("fallo"));

            IActionResult result = await _sut.GetUsersByIdSettings("u1", CancellationToken.None);

            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task UpdateUsersByIdSettings_ConExito_RetornaNoContent()
        {
            _userManagementMock.Setup(u => u.UpdateUserAsync("u1", It.IsAny<Auth0UserDto>())).ReturnsAsync(true);

            IActionResult result = await _sut.UpdateUsersByIdSettings("u1", new Auth0UserDto(), CancellationToken.None);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task UpdateUsersByIdSettings_SiFalla_RetornaProblemDetails500()
        {
            _userManagementMock.Setup(u => u.UpdateUserAsync("u1", It.IsAny<Auth0UserDto>())).ReturnsAsync(false);

            IActionResult result = await _sut.UpdateUsersByIdSettings("u1", new Auth0UserDto(), CancellationToken.None);

            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task UpdateUsersByIdSettings_ConCorreoTelefonoYRol_LosEnviaAlCasoDeUso()
        {
            var update = new Auth0UserDto { Email = "nuevo@correo.com", PhoneNumber = "+573113232323", RoleId = "rol_2" };
            _userManagementMock.Setup(u => u.UpdateUserAsync("u1", It.IsAny<Auth0UserDto>())).ReturnsAsync(true);

            IActionResult result = await _sut.UpdateUsersByIdSettings("u1", update, CancellationToken.None);

            result.Should().BeOfType<NoContentResult>();
            _userManagementMock.Verify(u => u.UpdateUserAsync("u1", It.Is<Auth0UserDto>(d => d.Email == "nuevo@correo.com" && d.PhoneNumber == "+573113232323" && d.RoleId == "rol_2")), Times.Once);
        }

        [Fact]
        public async Task UpdateUsersByIdSettings_SiAuth0Rechaza_RetornaProblemDetails500ConElMensaje()
        {
            _userManagementMock.Setup(u => u.UpdateUserAsync("u1", It.IsAny<Auth0UserDto>())).ThrowsAsync(new InvalidOperationException("rol inexistente"));

            IActionResult result = await _sut.UpdateUsersByIdSettings("u1", new Auth0UserDto { RoleId = "rol_x" }, CancellationToken.None);

            var problem = result.Should().BeOfType<ObjectResult>().Subject;
            problem.StatusCode.Should().Be(500);
            problem.Value.Should().BeOfType<ProblemDetails>().Which.Detail.Should().Be("rol inexistente");
        }

        [Fact]
        public async Task GetRolesListSettings_ConRoles_RetornaOkConElListado()
        {
            var roles = new List<Auth0RoleDto> { new() { Id = "rol_1", Name = "ADMIN" }, new() { Id = "rol_2", Name = "CLIENT" } };
            _userManagementMock.Setup(u => u.GetRolesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(roles);

            IActionResult result = await _sut.GetRolesListSettings(CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(roles);
        }

        [Fact]
        public async Task GetRolesListSettings_SiElCasoDeUsoDevuelveNull_RetornaProblemDetails500()
        {
            _userManagementMock.Setup(u => u.GetRolesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IList<Auth0RoleDto>)null!);

            IActionResult result = await _sut.GetRolesListSettings(CancellationToken.None);

            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetRolesListSettings_SiLanzaExcepcion_RetornaProblemDetails500()
        {
            _userManagementMock.Setup(u => u.GetRolesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("fallo Auth0"));

            IActionResult result = await _sut.GetRolesListSettings(CancellationToken.None);

            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UpdateUserStatusSettings_ConExito_RetornaNoContentYPasaElEstado(Boolean isActive)
        {
            _userManagementMock.Setup(u => u.SetUserStatusAsync("u1", isActive)).ReturnsAsync(true);

            IActionResult result = await _sut.UpdateUserStatusSettings("u1", new UsersManagementStatusRequest { IsActive = isActive }, CancellationToken.None);

            result.Should().BeOfType<NoContentResult>();
            _userManagementMock.Verify(u => u.SetUserStatusAsync("u1", isActive), Times.Once);
        }

        [Fact]
        public async Task UpdateUserStatusSettings_SiFalla_RetornaProblemDetails500()
        {
            _userManagementMock.Setup(u => u.SetUserStatusAsync("u1", It.IsAny<Boolean>())).ReturnsAsync(false);

            IActionResult result = await _sut.UpdateUserStatusSettings("u1", new UsersManagementStatusRequest { IsActive = true }, CancellationToken.None);

            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task UpdateUserStatusSettings_SiLanzaExcepcion_RetornaProblemDetails500()
        {
            _userManagementMock.Setup(u => u.SetUserStatusAsync(It.IsAny<String>(), It.IsAny<Boolean>())).ThrowsAsync(new InvalidOperationException("fallo Auth0"));

            IActionResult result = await _sut.UpdateUserStatusSettings("u1", new UsersManagementStatusRequest(), CancellationToken.None);

            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task DeleteUsersByIdSettings_ConExito_RetornaNoContent()
        {
            _userManagementMock.Setup(u => u.DeleteUserAsync("u1")).ReturnsAsync(true);

            IActionResult result = await _sut.DeleteUsersByIdSettings("u1", CancellationToken.None);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task DeleteUsersByIdSettings_SiFalla_RetornaProblemDetails500()
        {
            _userManagementMock.Setup(u => u.DeleteUserAsync("u1")).ReturnsAsync(false);

            IActionResult result = await _sut.DeleteUsersByIdSettings("u1", CancellationToken.None);

            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task CreateCustomerDataBase_RetornaCreatedAtRouteConElIdGenerado()
        {
            _customerUseCaseMock.Setup(u => u.CrearAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(77);

            var result = await _sut.CreateCustomerDataBase("123", CancellationToken.None);

            var created = result.Should().BeOfType<CreatedAtRouteResult>().Subject;
            created.RouteName.Should().Be(nameof(SettingsController.CreateCustomerDataBase));
            created.Value.Should().Be(77L);
        }

        [Fact]
        public async Task CreateCollaboratorDataBase_RetornaCreatedAtRouteConElIdGenerado()
        {
            _collaboratorUseCaseMock.Setup(u => u.CreateAsync("COL-1", It.IsAny<CancellationToken>())).ReturnsAsync(15);

            var result = await _sut.CreateCollaboratorDataBase("COL-1", CancellationToken.None);

            var created = result.Should().BeOfType<CreatedAtRouteResult>().Subject;
            created.RouteName.Should().Be(nameof(SettingsController.CreateCollaboratorDataBase));
            created.Value.Should().Be(15L);
        }

        [Fact]
        public async Task CreateCustomerCollaboratorDataBase_RetornaCreatedAtRouteConElIdGenerado()
        {
            _collaboratorUseCaseMock.Setup(u => u.CreateCustomerCollaboratorAsync("CUS-1", "COL-1", It.IsAny<CancellationToken>())).ReturnsAsync(99);

            var result = await _sut.CreateCustomerCollaboratorDataBase("CUS-1", "COL-1", CancellationToken.None);

            var created = result.Should().BeOfType<CreatedAtRouteResult>().Subject;
            created.RouteName.Should().Be(nameof(SettingsController.CreateCustomerCollaboratorDataBase));
            created.Value.Should().Be(99L);
        }

        [Fact]
        public async Task CreateCustomerCollaboratorDataBase_PropagaLaExcepcionDelUseCase()
        {
            _collaboratorUseCaseMock
                .Setup(u => u.CreateCustomerCollaboratorAsync(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("cliente no existe"));

            Func<Task> act = () => _sut.CreateCustomerCollaboratorDataBase("CUS-X", "COL-1", CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task ListCustomersAllDataBase_RetornaOkConElListadoDelCasoDeUso()
        {
            var customers = new List<CustomerDtoResult> { new() { Identificacion = "900111" }, new() { Identificacion = "900222" } };
            _customerUseCaseMock.Setup(u => u.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(customers);

            var result = await _sut.ListCustomersAllDataBase(CancellationToken.None);

            result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(customers);
        }

        [Fact]
        public async Task ListCustomersAllDataBase_SinClientes_RetornaOkConListaVacia()
        {
            _customerUseCaseMock.Setup(u => u.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<CustomerDtoResult>());

            var result = await _sut.ListCustomersAllDataBase(CancellationToken.None);

            result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeAssignableTo<IEnumerable<CustomerDtoResult>>().Which.Should().BeEmpty();
        }

        [Fact]
        public async Task ListCollaboratorDataBase_RetornaOkConElListadoDelCasoDeUso()
        {
            var collaborators = new List<CollaboratorDtoResult> { new() { Identificacion = "COL-1" } };
            _collaboratorUseCaseMock.Setup(u => u.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(collaborators);

            var result = await _sut.ListCollaboratorDataBase(CancellationToken.None);

            result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(collaborators);
        }

        [Fact]
        public async Task ListCustomerCollaboratorDataBase_RetornaOkConElListadoDelCasoDeUso()
        {
            var relations = new List<CustomerCollaboratorRelationDtoResult>
            {
                new() { IdentificacionCustomer = "CUS-1", IdentificacionCollaborator = "COL-1" }
            };
            _collaboratorUseCaseMock.Setup(u => u.ListCustomerCollaboratorAsync(It.IsAny<CancellationToken>())).ReturnsAsync(relations);

            var result = await _sut.ListCustomerCollaboratorDataBase(CancellationToken.None);

            result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(relations);
        }
    }
}
