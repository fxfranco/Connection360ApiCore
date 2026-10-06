using Microsoft.AspNetCore.Mvc.Routing;
using Asp.Versioning;
using Connection360Notification.Api.Controllers;
using Connection360Notification.Application.DTOs;
using Connection360Notification.Application.Ports;
using Connection360Notification.Application.Ports.Inbound;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Reflection;
using Xunit;

namespace Connection360Notification.Api.Tests.Controllers
{
    public class NotificationsControllerComportamientoTests
    {
        private readonly Mock<IGetNotificationsUseCase> _useCaseMock = new();
        private readonly Mock<INotificationUseCase> _notificationUseCaseMock = new();
        private readonly NotificationsController _sut;

        public NotificationsControllerComportamientoTests()
        {
            _sut = new NotificationsController(_useCaseMock.Object, _notificationUseCaseMock.Object);
        }

        // ---------- GetAllNotifications (por cliente) ----------

        [Fact]
        public async Task GetAllNotifications_ConIdClient_RetornaOkConLaListaDelUseCase()
        {
            var expected = new List<NotificationsListResponse>
            {
                new() { IdNotification = 1, ClientId = "123", Title = "Hola" },
                new() { IdNotification = 2, ClientId = "123", Title = "Chao" }
            };
            _useCaseMock
                .Setup(u => u.ExecuteGetNotificationsByClientAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            IActionResult result = await _sut.GetAllNotifications("123", CancellationToken.None);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeSameAs(expected);
        }

        [Fact]
        public async Task GetAllNotifications_ConstruyeElRequestConElIdClientYRoleNameVacio()
        {
            ClientSummaryRequest? captured = null;
            _useCaseMock
                .Setup(u => u.ExecuteGetNotificationsByClientAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>()))
                .Callback<ClientSummaryRequest, CancellationToken>((r, _) => captured = r)
                .ReturnsAsync(new List<NotificationsListResponse>());

            await _sut.GetAllNotifications("cliente-77", CancellationToken.None);

            captured.Should().NotBeNull();
            captured!.IdClient.Should().Be("cliente-77");
            captured.RoleName.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllNotifications_PropagaElCancellationTokenAlUseCase()
        {
            using var cts = new CancellationTokenSource();
            _useCaseMock
                .Setup(u => u.ExecuteGetNotificationsByClientAsync(It.IsAny<ClientSummaryRequest>(), cts.Token))
                .ReturnsAsync(new List<NotificationsListResponse>());

            await _sut.GetAllNotifications("1", cts.Token);

            _useCaseMock.Verify(u => u.ExecuteGetNotificationsByClientAsync(It.IsAny<ClientSummaryRequest>(), cts.Token), Times.Once);
        }

        [Fact]
        public async Task GetAllNotifications_ConListaVacia_RetornaOkConListaVacia()
        {
            _useCaseMock
                .Setup(u => u.ExecuteGetNotificationsByClientAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<NotificationsListResponse>());

            IActionResult result = await _sut.GetAllNotifications("1", CancellationToken.None);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeAssignableTo<IEnumerable<NotificationsListResponse>>().Which.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllNotifications_CuandoElUseCaseLanzaExcepcion_LaPropaga()
        {
            _useCaseMock
                .Setup(u => u.ExecuteGetNotificationsByClientAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("fallo"));

            Func<Task> act = () => _sut.GetAllNotifications("1", CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("fallo");
        }

        [Fact]
        public async Task GetAllNotifications_CuandoSeCancela_PropagaOperationCanceledException()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            _useCaseMock
                .Setup(u => u.ExecuteGetNotificationsByClientAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            Func<Task> act = () => _sut.GetAllNotifications("1", cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        // ---------- UpdateReadNotification ----------

        [Fact]
        public async Task UpdateReadNotification_ConstruyeElRequestConLosParametrosRecibidos()
        {
            NotificationsRequest? captured = null;
            _useCaseMock
                .Setup(u => u.ExecuteMarkAsReadAsync(It.IsAny<NotificationsRequest>(), It.IsAny<CancellationToken>()))
                .Callback<NotificationsRequest, CancellationToken>((r, _) => captured = r)
                .ReturnsAsync(true);

            await _sut.UpdateReadNotification("cliente-9", 42, CancellationToken.None);

            captured.Should().NotBeNull();
            captured!.IdClient.Should().Be("cliente-9");
            captured.IdNotification.Should().Be(42);
            captured.RoleName.Should().BeEmpty();
        }

        [Fact]
        public async Task UpdateReadNotification_PropagaElCancellationTokenAlUseCase()
        {
            using var cts = new CancellationTokenSource();
            _useCaseMock
                .Setup(u => u.ExecuteMarkAsReadAsync(It.IsAny<NotificationsRequest>(), cts.Token))
                .ReturnsAsync(true);

            await _sut.UpdateReadNotification("1", 1, cts.Token);

            _useCaseMock.Verify(u => u.ExecuteMarkAsReadAsync(It.IsAny<NotificationsRequest>(), cts.Token), Times.Once);
        }

        [Fact]
        public async Task UpdateReadNotification_CuandoElUseCaseLanzaExcepcion_LaPropaga()
        {
            _useCaseMock
                .Setup(u => u.ExecuteMarkAsReadAsync(It.IsAny<NotificationsRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new KeyNotFoundException("no existe"));

            Func<Task> act = () => _sut.UpdateReadNotification("1", 1, CancellationToken.None);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        // ---------- GetAllNotificationsDB (endpoint de prueba) ----------

        [Fact]
        public async Task GetAllNotificationsDB_PropagaElCancellationTokenAlUseCase()
        {
            using var cts = new CancellationTokenSource();
            _useCaseMock
                .Setup(u => u.ExecuteGetNotificationsAllAsync(cts.Token))
                .ReturnsAsync(new List<NotificationsListResponse>());

            await _sut.GetAllNotificationsDB(cts.Token);

            _useCaseMock.Verify(u => u.ExecuteGetNotificationsAllAsync(cts.Token), Times.Once);
        }

        [Fact]
        public async Task GetAllNotificationsDB_ConListaVacia_RetornaOkConListaVacia()
        {
            _useCaseMock
                .Setup(u => u.ExecuteGetNotificationsAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<NotificationsListResponse>());

            IActionResult result = await _sut.GetAllNotificationsDB(CancellationToken.None);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeAssignableTo<IEnumerable<NotificationsListResponse>>().Which.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllNotificationsDB_CuandoElUseCaseLanzaExcepcion_LaPropaga()
        {
            _useCaseMock
                .Setup(u => u.ExecuteGetNotificationsAllAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("bd caida"));

            Func<Task> act = () => _sut.GetAllNotificationsDB(CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task Controlador_NoInteractuaConElCasoDeUsoDeEnvioDeNotificaciones()
        {
            _useCaseMock
                .Setup(u => u.ExecuteGetNotificationsAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<NotificationsListResponse>());

            await _sut.GetAllNotificationsDB(CancellationToken.None);

            _notificationUseCaseMock.VerifyNoOtherCalls();
        }

        // ---------- Metadatos (atributos) del controlador ----------

        [Fact]
        public void Controlador_TieneLosAtributosDeApiYSeguridadEsperados()
        {
            Type type = typeof(NotificationsController);

            type.GetCustomAttribute<ApiControllerAttribute>().Should().NotBeNull();
            type.GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();
            type.GetCustomAttribute<RouteAttribute>()!.Template.Should().Be("api/v{version:apiVersion}/notifications");
            type.GetCustomAttribute<ApiVersionAttribute>()!.Versions.Should().ContainSingle().Which.Should().Be(new ApiVersion(1, 0));
            type.GetCustomAttribute<ProducesAttribute>()!.ContentTypes.Should().Contain("application/json");
            typeof(ControllerBase).IsAssignableFrom(type).Should().BeTrue();
        }

        [Theory]
        [InlineData(nameof(NotificationsController.GetAllNotifications), typeof(HttpGetAttribute), "allnotifications")]
        [InlineData(nameof(NotificationsController.UpdateReadNotification), typeof(HttpPatchAttribute), "readnotification/{idClient}/{idNotification}")]
        [InlineData(nameof(NotificationsController.GetAllNotificationsDB), typeof(HttpGetAttribute), "allnotificationslistTest")]
        public void Metodos_TienenElVerboYLaRutaHttpEsperados(String methodName, Type attributeType, String template)
        {
            MethodInfo method = typeof(NotificationsController).GetMethod(methodName)!;

            var attribute = method.GetCustomAttribute(attributeType).Should().BeAssignableTo<HttpMethodAttribute>().Subject;

            attribute.Template.Should().Be(template);
        }

        [Theory]
        [InlineData(nameof(NotificationsController.GetAllNotifications))]
        [InlineData(nameof(NotificationsController.UpdateReadNotification))]
        public void Metodos_Protegidos_RequierenLosRolesDeNegocio(String methodName)
        {
            MethodInfo method = typeof(NotificationsController).GetMethod(methodName)!;

            var authorize = method.GetCustomAttribute<AuthorizeAttribute>();

            authorize.Should().NotBeNull();
            authorize!.Roles.Should().Be("ADMIN,CLIENT,ANALISTAOPE,ANALISTASAC");
        }

        [Fact]
        public void GetAllNotificationsDB_PermiteAccesoAnonimo()
        {
            MethodInfo method = typeof(NotificationsController).GetMethod(nameof(NotificationsController.GetAllNotificationsDB))!;

            method.GetCustomAttribute<AllowAnonymousAttribute>().Should().NotBeNull();
        }

        [Fact]
        public void Controlador_ExponeExactamenteTresAccionesPublicas()
        {
            MethodInfo[] actions = typeof(NotificationsController)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            actions.Select(a => a.Name).Should().BeEquivalentTo(
                nameof(NotificationsController.GetAllNotifications),
                nameof(NotificationsController.UpdateReadNotification),
                nameof(NotificationsController.GetAllNotificationsDB));
        }

        [Fact]
        public void Constructor_ConDependencias_CreaLaInstancia()
        {
            var controller = new NotificationsController(_useCaseMock.Object, _notificationUseCaseMock.Object);

            controller.Should().NotBeNull();
            controller.Should().BeAssignableTo<ControllerBase>();
        }
    }
}
