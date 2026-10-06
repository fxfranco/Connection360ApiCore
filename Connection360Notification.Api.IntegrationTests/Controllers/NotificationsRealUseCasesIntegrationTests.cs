using Connection360Notification.Api.IntegrationTests.Infrastructure;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Enums;
using FluentAssertions;
using Moq;
using System.Globalization;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Connection360Notification.Api.IntegrationTests.Controllers
{
    /// <summary>
    /// Flujo completo controlador -> caso de uso real -> mapeo -> filtro de respuesta, simulando solo el repositorio.
    /// </summary>
    public class NotificationsRealUseCasesIntegrationTests : IClassFixture<RealUseCasesWebApplicationFactory>
    {
        private readonly RealUseCasesWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public NotificationsRealUseCasesIntegrationTests(RealUseCasesWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.ResetMocks();
            _client = _factory.CreateClient();
        }

        private static HttpRequestMessage Authorized(HttpMethod method, String url)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("X-Test-Auth", "true");
            request.Headers.Add("X-Test-Roles", "CLIENT");
            return request;
        }

        private static NotificationMessage Notification(String clientId, String title, DateTime notificationDate, Int64 idNotification, NotificationType type = NotificationType.Comment)
            => new(clientId, type, "mensaje " + title, "DOC-1", title,
                   new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), NotificationStatus.Unread, notificationDate, idNotification);

        [Fact]
        public async Task GetAllNotifications_ConNotificacionesDelCliente_LasMapeaYLasOrdenaDeMasRecienteAMasAntigua()
        {
            var older = Notification("c1", "antigua", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), 1);
            var newer = Notification("c1", "reciente", new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc), 2, NotificationType.ChangeState);
            _factory.NotificationRepositoryMock
                .Setup(r => r.GetByClientAsync("c1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<NotificationMessage> { older, newer });

            HttpResponseMessage response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/notifications/allnotifications?idClient=c1"));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            JsonElement data = doc.RootElement.GetProperty("dataResponse");
            data.GetArrayLength().Should().Be(2);
            data[0].GetProperty("title").GetString().Should().Be("reciente");
            data[0].GetProperty("idNotification").GetInt64().Should().Be(2);
            data[0].GetProperty("clientId").GetString().Should().Be("c1");
            data[0].GetProperty("documentNumber").GetString().Should().Be("DOC-1");
            data[0].GetProperty("notificationType").GetInt32().Should().Be((Int32)Connection360Notification.Application.Enum.NotificationType.ChangeState);
            data[0].GetProperty("notificationStatus").GetInt32().Should().Be((Int32)Connection360Notification.Application.Enum.NotificationStatus.Unread);
            DateTime.Parse(data[0].GetProperty("notificationDate").GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
                .Year.Should().Be(2024);
            data[1].GetProperty("title").GetString().Should().Be("antigua");
        }

        [Fact]
        public async Task GetAllNotifications_ClienteSinNotificaciones_RetornaListaVacia()
        {
            _factory.NotificationRepositoryMock
                .Setup(r => r.GetByClientAsync(It.IsAny<String>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<NotificationMessage>());

            HttpResponseMessage response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/notifications/allnotifications?idClient=nadie"));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            doc.RootElement.GetProperty("dataResponse").GetArrayLength().Should().Be(0);
        }

        [Fact]
        public async Task GetAllNotifications_ConIdClientEnBlanco_NoConsultaElRepositorio()
        {
            HttpResponseMessage response = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/v1/notifications/allnotifications?idClient=%20"));

            // La validacion implicita [Required] (400) o el caso de uso (lista vacia) deben evitar la consulta.
            ((Int32)response.StatusCode).Should().BeOneOf(200, 400);
            _factory.NotificationRepositoryMock.Verify(r => r.GetByClientAsync(It.IsAny<String>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateReadNotification_ConNotificacionExistente_InvocaAlRepositorioYRetorna204()
        {
            _factory.NotificationRepositoryMock
                .Setup(r => r.MarkAsReadAsync("c1", 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            HttpResponseMessage response = await _client.SendAsync(Authorized(HttpMethod.Patch, "/api/v1/notifications/readnotification/c1/10"));

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            _factory.NotificationRepositoryMock.Verify(r => r.MarkAsReadAsync("c1", 10, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateReadNotification_ConNotificacionInexistente_Retorna404()
        {
            _factory.NotificationRepositoryMock
                .Setup(r => r.MarkAsReadAsync(It.IsAny<String>(), It.IsAny<Int64>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            HttpResponseMessage response = await _client.SendAsync(Authorized(HttpMethod.Patch, "/api/v1/notifications/readnotification/c1/999"));

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetAllNotificationsDB_ListaTodasLasNotificacionesSinAutenticacion()
        {
            _factory.NotificationRepositoryMock
                .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<NotificationMessage>
                {
                    Notification("c1", "a", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), 1),
                    Notification("c2", "b", new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc), 2)
                });

            HttpResponseMessage response = await _client.GetAsync("/api/v1/notifications/allnotificationslistTest");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            JsonElement data = doc.RootElement.GetProperty("dataResponse");
            data.GetArrayLength().Should().Be(2);
            data[0].GetProperty("clientId").GetString().Should().Be("c2");
        }

        [Fact]
        public async Task GetAllNotificationsDB_CuandoElRepositorioFalla_Retorna500ConElEnvoltorioDeError()
        {
            _factory.NotificationRepositoryMock
                .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("mongo caido"));

            HttpResponseMessage response = await _client.GetAsync("/api/v1/notifications/allnotificationslistTest");

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            doc.RootElement.GetProperty("message").GetString().Should().Be("Error interno del servidor");
            doc.RootElement.GetProperty("error").GetString().Should().Be("mongo caido");
        }
    }
}
