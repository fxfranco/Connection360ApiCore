using Connection360.Api.Controllers;
using Connection360.Api.Models;
using Connection360.Api.Tests.TestSupport;
using Connection360.Application.DTOs;
using Connection360.Application.DTOs.Persistence;
using Connection360.Application.Ports;
using Connection360.Application.Ports.Persistence;
using Connection360.Domain.Dtos;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Connection360.Api.Tests.Controllers
{
    /// <summary>
    /// Rutas de autorizacion (403) y ramas adicionales de <see cref="SettingsController"/>.
    /// </summary>
    public class SettingsControllerAccessTests
    {
        private const String MensajeDenegado = "Acceso denegado. No se tiene un rol asignado.";

        private readonly Mock<IGetUserManagementUseCase> _userManagementMock = new();
        private readonly Mock<ICustomerUseCase> _customerUseCaseMock = new();
        private readonly Mock<ICustomerNotificationsSettingsUseCase> _notificationsSettingsMock = new();
        private readonly Mock<IMasterSettingsUseCase> _masterSettingsMock = new();
        private readonly Mock<ICollaboratorUseCase> _collaboratorUseCaseMock = new();
        private readonly Mock<IOutboxMessagesUseCase> _outboxMessagesUseCase = new();

        private static String[] Split(String csv) => csv.Split(',', StringSplitOptions.RemoveEmptyEntries);

        private SettingsController BuildSut(params String[] roles)
        {
            return new SettingsController(_userManagementMock.Object, _customerUseCaseMock.Object, _notificationsSettingsMock.Object,
                _masterSettingsMock.Object, _collaboratorUseCaseMock.Object, _outboxMessagesUseCase.Object)
            {
                ControllerContext = ControllerContextFactory.Create(roles)
            };
        }

        private static void AssertForbidden(IActionResult result)
        {
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
            objectResult.Value.Should().Be(MensajeDenegado);
        }

        // ---------- Notificaciones: usuario sin rol reconocido -> 403 ----------

        [Fact]
        public async Task GetNotificationsSettings_SinRolAsignado_Retorna403SinLlamarAlUseCase()
        {
            SettingsController sut = BuildSut();

            IActionResult result = await sut.GetNotificationsSettings("123", CancellationToken.None);

            AssertForbidden(result);
            _notificationsSettingsMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetNotificationsSettings_ConRolDesconocido_Retorna403()
        {
            SettingsController sut = BuildSut("OTRO_ROL");

            IActionResult result = await sut.GetNotificationsSettings("123", CancellationToken.None);

            AssertForbidden(result);
        }

        [Fact]
        public async Task CreateNotificationsSettings_SinRolAsignado_Retorna403SinLlamarAlUseCase()
        {
            SettingsController sut = BuildSut();

            IActionResult result = await sut.CreateNotificationsSettings(new CustomerNotificationsSettingsResponse(), CancellationToken.None);

            AssertForbidden(result);
            _notificationsSettingsMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UpdateNotificationsSettings_SinRolAsignado_Retorna403SinLlamarAlUseCase()
        {
            SettingsController sut = BuildSut();

            IActionResult result = await sut.UpdateNotificationsSettings(new CustomerNotificationsSettingsResponse(), CancellationToken.None);

            AssertForbidden(result);
            _notificationsSettingsMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("ADMIN")]
        [InlineData("CLIENT")]
        [InlineData("ANALISTAOPE")]
        [InlineData("ANALISTASAC")]
        public async Task GetNotificationsSettings_ConCualquierRolValido_RetornaOk(String role)
        {
            var expected = new CustomerNotificationsSettingsResponse();
            _notificationsSettingsMock.Setup(u => u.GetCustomerNotificationSettings("123", It.IsAny<CancellationToken>())).ReturnsAsync(expected);
            SettingsController sut = BuildSut(role);

            IActionResult result = await sut.GetNotificationsSettings("123", CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(expected);
        }

        [Theory]
        [InlineData("CLIENT")]
        [InlineData("ANALISTAOPE")]
        [InlineData("ANALISTASAC")]
        public async Task CreateYUpdateNotificationsSettings_ConRolNoAdminValido_NoRetornan403(String role)
        {
            var response = new CustomerNotificationsSettingsResponse
            {
                NotificationChannels = new NotificationChannelsResponse { NotificationChannelId = 1 },
                NotificationEvents = new NotificationEventsResponse { NotificationEventId = 2 }
            };
            _notificationsSettingsMock.Setup(u => u.CreateCustomerNotificationSettings(It.IsAny<CustomerNotificationsSettingsResponse>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);
            _notificationsSettingsMock.Setup(u => u.UpdateCustomerNotificationSettings(It.IsAny<CustomerNotificationsSettingsResponse>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            SettingsController sut = BuildSut(role);

            IActionResult created = await sut.CreateNotificationsSettings(response, CancellationToken.None);
            IActionResult updated = await sut.UpdateNotificationsSettings(response, CancellationToken.None);

            created.Should().BeOfType<CreatedAtRouteResult>();
            updated.Should().BeOfType<NoContentResult>();
        }

        // ---------- Configuracion maestra y usuarios: solo ADMIN ----------

        [Theory]
        [InlineData("")]
        [InlineData("CLIENT")]
        [InlineData("ANALISTAOPE")]
        [InlineData("ANALISTASAC")]
        public async Task GetMasterSettings_SinSerAdmin_Retorna403(String rolesCsv)
        {
            SettingsController sut = BuildSut(Split(rolesCsv));

            IActionResult result = await sut.GetMasterSettings(CancellationToken.None);

            AssertForbidden(result);
            _masterSettingsMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("")]
        [InlineData("CLIENT")]
        public async Task CreateMasterSettings_SinSerAdmin_Retorna403(String rolesCsv)
        {
            SettingsController sut = BuildSut(Split(rolesCsv));
            var dto = new CreateMasterSettingsDto(true, false, true, "COP", "es", "America/Bogota", 30);

            IActionResult result = await sut.CreateMasterSettings(dto, CancellationToken.None);

            AssertForbidden(result);
            _masterSettingsMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("")]
        [InlineData("CLIENT")]
        public async Task UpdateMasterSettings_SinSerAdmin_Retorna403(String rolesCsv)
        {
            SettingsController sut = BuildSut(Split(rolesCsv));
            var dto = new MasterSettingsResponseDto(1, true, false, true, "COP", "es", "America/Bogota", 30);

            IActionResult result = await sut.UpdateMasterSettings(dto, CancellationToken.None);

            AssertForbidden(result);
            _masterSettingsMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("")]
        [InlineData("CLIENT")]
        public async Task GetUsersListSettings_SinSerAdmin_Retorna403(String rolesCsv)
        {
            SettingsController sut = BuildSut(Split(rolesCsv));

            IActionResult result = await sut.GetUsersListSettings(1, 10, CancellationToken.None);

            AssertForbidden(result);
            _userManagementMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("")]
        [InlineData("ANALISTASAC")]
        public async Task GetUsersByIdSettings_SinSerAdmin_Retorna403(String rolesCsv)
        {
            SettingsController sut = BuildSut(Split(rolesCsv));

            IActionResult result = await sut.GetUsersByIdSettings("u1", CancellationToken.None);

            AssertForbidden(result);
            _userManagementMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("")]
        [InlineData("ANALISTAOPE")]
        public async Task UpdateUsersByIdSettings_SinSerAdmin_Retorna403(String rolesCsv)
        {
            SettingsController sut = BuildSut(Split(rolesCsv));

            IActionResult result = await sut.UpdateUsersByIdSettings("u1", new Auth0UserDto(), CancellationToken.None);

            AssertForbidden(result);
            _userManagementMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("")]
        [InlineData("CLIENT")]
        public async Task DeleteUsersByIdSettings_SinSerAdmin_Retorna403(String rolesCsv)
        {
            SettingsController sut = BuildSut(Split(rolesCsv));

            IActionResult result = await sut.DeleteUsersByIdSettings("u1", CancellationToken.None);

            AssertForbidden(result);
            _userManagementMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetMasterSettings_ConVariosRolesIncluidoAdmin_UsaElPrimeroDelEnumYPermite()
        {
            // ADMIN precede a CLIENT en el enum, por lo que FirstOrDefault resuelve ADMIN.
            _masterSettingsMock.Setup(u => u.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new MasterSettingsResponse { IdMasterSettings = 3 });
            SettingsController sut = BuildSut("CLIENT", "ADMIN");

            IActionResult result = await sut.GetMasterSettings(CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetMasterSettings_ConClientYAnalistaPeroSinAdmin_Retorna403()
        {
            SettingsController sut = BuildSut("CLIENT", "ANALISTAOPE");

            IActionResult result = await sut.GetMasterSettings(CancellationToken.None);

            AssertForbidden(result);
        }

        // ---------- Ramas adicionales del listado/consulta de usuarios ----------

        [Fact]
        public async Task GetUsersListSettings_ConPaginaCero_NoLaDecrementa()
        {
            _userManagementMock.Setup(u => u.GetAllUsersAsync(It.IsAny<UsersManagementRequest>())).ReturnsAsync(new List<Auth0UserDto>());
            SettingsController sut = BuildSut("ADMIN");

            await sut.GetUsersListSettings(0, 5, CancellationToken.None);

            _userManagementMock.Verify(u => u.GetAllUsersAsync(It.Is<UsersManagementRequest>(r => r.Page == 0 && r.Size == 5 && r.RoleName == String.Empty)), Times.Once);
        }

        [Fact]
        public async Task GetUsersListSettings_ConPaginaNegativa_NoLaDecrementa()
        {
            _userManagementMock.Setup(u => u.GetAllUsersAsync(It.IsAny<UsersManagementRequest>())).ReturnsAsync(new List<Auth0UserDto>());
            SettingsController sut = BuildSut("ADMIN");

            await sut.GetUsersListSettings(-2, 5, CancellationToken.None);

            _userManagementMock.Verify(u => u.GetAllUsersAsync(It.Is<UsersManagementRequest>(r => r.Page == -2)), Times.Once);
        }

        [Fact]
        public async Task GetUsersListSettings_ConResultadoValido_ArmaElPagedResultConLosParametros()
        {
            var users = new List<Auth0UserDto> { new() { UserId = "u1" } };
            _userManagementMock.Setup(u => u.GetAllUsersAsync(It.IsAny<UsersManagementRequest>())).ReturnsAsync(users);
            SettingsController sut = BuildSut("ADMIN");

            IActionResult result = await sut.GetUsersListSettings(4, 25, CancellationToken.None);

            var paged = result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<PagedResult<Object>>().Subject;
            paged.CurrentPage.Should().Be(3);
            paged.Limit.Should().Be(25);
            paged.Items.Should().ContainSingle().Which.Should().BeSameAs(users);
        }

        [Fact]
        public async Task GetUsersListSettings_SiElUseCaseRetornaNull_RetornaProblemDetails500ConMensajeGenerico()
        {
            _userManagementMock.Setup(u => u.GetAllUsersAsync(It.IsAny<UsersManagementRequest>())).ReturnsAsync((IList<Auth0UserDto>)null!);
            SettingsController sut = BuildSut("ADMIN");

            IActionResult result = await sut.GetUsersListSettings(1, 10, CancellationToken.None);

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(500);
            var problem = objectResult.Value.Should().BeAssignableTo<ProblemDetails>().Subject;
            problem.Detail.Should().Be("No se pudo realizar el proceso. Intente más tarde.");
            problem.Title.Should().Be("Error al listar usuarios en el servidor");
        }

        [Fact]
        public async Task GetUsersListSettings_SiElUseCaseLanzaExcepcion_IncluyeElMensajeEnElDetalle()
        {
            _userManagementMock.Setup(u => u.GetAllUsersAsync(It.IsAny<UsersManagementRequest>())).ThrowsAsync(new InvalidOperationException("fallo Auth0"));
            SettingsController sut = BuildSut("ADMIN");

            IActionResult result = await sut.GetUsersListSettings(1, 10, CancellationToken.None);

            var problem = result.Should().BeOfType<ObjectResult>().Which.Value.Should().BeAssignableTo<ProblemDetails>().Subject;
            problem.Detail.Should().Be("fallo Auth0");
        }

        [Fact]
        public async Task GetUsersByIdSettings_SiNoEncuentraElUsuario_RetornaProblemDetails500()
        {
            _userManagementMock.Setup(u => u.GetUsersByIdAsync("u1")).ReturnsAsync((Auth0UserDto)null!);
            SettingsController sut = BuildSut("ADMIN");

            IActionResult result = await sut.GetUsersByIdSettings("u1", CancellationToken.None);

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(500);
            objectResult.Value.Should().BeAssignableTo<ProblemDetails>().Which.Detail.Should().Be("No se pudo realizar el proceso. Intente más tarde.");
        }

        [Fact]
        public async Task GetUsersByIdSettings_SiLanzaExcepcion_IncluyeElMensajeEnElDetalle()
        {
            _userManagementMock.Setup(u => u.GetUsersByIdAsync("u1")).ThrowsAsync(new InvalidOperationException("fallo"));
            SettingsController sut = BuildSut("ADMIN");

            IActionResult result = await sut.GetUsersByIdSettings("u1", CancellationToken.None);

            result.Should().BeOfType<ObjectResult>().Which.Value.Should().BeAssignableTo<ProblemDetails>().Which.Detail.Should().Be("fallo");
        }

        [Fact]
        public async Task UpdateUsersByIdSettings_SiElUseCaseLanzaExcepcion_RetornaProblemDetails500()
        {
            _userManagementMock.Setup(u => u.UpdateUserAsync("u1", It.IsAny<Auth0UserDto>())).ThrowsAsync(new InvalidOperationException("boom"));
            SettingsController sut = BuildSut("ADMIN");

            IActionResult result = await sut.UpdateUsersByIdSettings("u1", new Auth0UserDto(), CancellationToken.None);

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(500);
            objectResult.Value.Should().BeAssignableTo<ProblemDetails>().Which.Detail.Should().Be("boom");
        }

        [Fact]
        public async Task DeleteUsersByIdSettings_SiElUseCaseLanzaExcepcion_RetornaProblemDetails500()
        {
            _userManagementMock.Setup(u => u.DeleteUserAsync("u1")).ThrowsAsync(new InvalidOperationException("boom"));
            SettingsController sut = BuildSut("ADMIN");

            IActionResult result = await sut.DeleteUsersByIdSettings("u1", CancellationToken.None);

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(500);
            objectResult.Value.Should().BeAssignableTo<ProblemDetails>().Which.Detail.Should().Be("boom");
        }

        [Fact]
        public async Task UpdateUsersByIdSettings_PropagaElIdYElDtoAlUseCase()
        {
            var dto = new Auth0UserDto { UserId = "u9" };
            _userManagementMock.Setup(u => u.UpdateUserAsync("u9", dto)).ReturnsAsync(true);
            SettingsController sut = BuildSut("ADMIN");

            await sut.UpdateUsersByIdSettings("u9", dto, CancellationToken.None);

            _userManagementMock.Verify(u => u.UpdateUserAsync("u9", dto), Times.Once);
        }

        // ---------- Outbox de prueba ----------

        [Fact]
        public async Task GeneratenotificationOutbox_ConExito_RetornaOkConTrue()
        {
            var dto = new CreateOutboxMessagesDto("CLI-1", "EVT", "DOC-1", "Titulo", "Mensaje");
            _outboxMessagesUseCase.Setup(u => u.CreateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            SettingsController sut = BuildSut();

            ActionResult result = await sut.GeneratenotificationOutbox(dto, CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(true);
        }

        [Fact]
        public async Task GeneratenotificationOutbox_SiFalla_RetornaProblemDetails500()
        {
            var dto = new CreateOutboxMessagesDto("CLI-1", "EVT", "DOC-1", "Titulo", "Mensaje");
            _outboxMessagesUseCase.Setup(u => u.CreateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            SettingsController sut = BuildSut();

            ActionResult result = await sut.GeneratenotificationOutbox(dto, CancellationToken.None);

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(500);
            objectResult.Value.Should().BeAssignableTo<ProblemDetails>().Which.Title.Should().Be("Error al generar notificacion outbox en el api core");
        }

        [Fact]
        public async Task CreateCustomerDataBase_PropagaLaExcepcionDelUseCase()
        {
            _customerUseCaseMock.Setup(u => u.CrearAsync(It.IsAny<String>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("db"));
            SettingsController sut = BuildSut();

            Func<Task> act = () => sut.CreateCustomerDataBase("X", CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task CreateCollaboratorDataBase_PropagaLaExcepcionDelUseCase()
        {
            _collaboratorUseCaseMock.Setup(u => u.CreateAsync(It.IsAny<String>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("db"));
            SettingsController sut = BuildSut();

            Func<Task> act = () => sut.CreateCollaboratorDataBase("X", CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
