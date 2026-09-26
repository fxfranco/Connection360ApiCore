using Connection360.Api.Controllers;
using Connection360.Api.Models;
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
            _sut = new SettingsController(_userManagementMock.Object, _customerUseCaseMock.Object, _notificationsSettingsMock.Object, _masterSettingsMock.Object, _collaboratorUseCaseMock.Object, _outboxMessagesUseCase.Object);
        }

        [Fact]
        public async Task GetNotificationsSettings_RetornaOkConElResultado()
        {
            var expected = new CustomerNotificationsSettingsResponse();
            _notificationsSettingsMock.Setup(u => u.GetCustomerNotificationSettings("123", It.IsAny<CancellationToken>())).ReturnsAsync(expected);

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
            _notificationsSettingsMock.Setup(u => u.CreateCustomerNotificationSettings(It.IsAny<CustomerNotificationsSettingsResponse>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            IActionResult result = await _sut.CreateNotificationsSettings(new CustomerNotificationsSettingsResponse(), CancellationToken.None);

            var created = result.Should().BeOfType<CreatedAtRouteResult>().Subject;
            created.RouteName.Should().Be(nameof(SettingsController.GetNotificationsSettings));
            created.Value.Should().BeSameAs(expected);
        }

        [Fact]
        public async Task UpdateNotificationsSettings_RetornaNoContent()
        {
            _notificationsSettingsMock.Setup(u => u.UpdateCustomerNotificationSettings(It.IsAny<CustomerNotificationsSettingsResponse>(), It.IsAny<CancellationToken>()))
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
            created.RouteName.Should().Be(nameof(SettingsController.GetMasterSettings));
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
    }
}
