using Connection360.Api.IntegrationTests.Infrastructure;
using Connection360.Application.DTOs;
using Connection360.Application.Ports;
using FluentAssertions;
using Moq;
using System.Net;
using Xunit;

namespace Connection360.Api.IntegrationTests.Controllers
{
    public class NotificationsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public NotificationsControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.ResetMocks();
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetAllNotifications_SinAutenticacion_Retorna401()
        {
            HttpResponseMessage response = await _client.GetAsync("/api/v1/notifications/allnotifications?idClient=123");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetAllNotifications_ConRolValido_Retorna200()
        {
            _factory.NotificationsUseCaseMock
                .Setup(u => u.ExecuteGetNotificationsAllAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>()))
                .Returns(new List<NotificationsListResponse>());

            var request = AuthorizedRequest(HttpMethod.Get, "/api/v1/notifications/allnotifications?idClient=123", roles: "CLIENT");

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GenerateNotifications_EsPublicoPorAllowAnonymousDelMetodo_EnviaLaNotificacionYRetorna200()
        {
            // El metodo conserva [AllowAnonymous], que prevalece sobre el [Authorize] de la clase.
            HttpResponseMessage response = await _client.GetAsync("/api/v1/notifications/generatenotifications?idClient=123&Message=Hola");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            _factory.NotifierServiceMock.Verify(n => n.SendNotificationToUserAsync("123", "Hola", It.IsAny<Object>()), Times.Once);
        }

        [Fact]
        public async Task UpdateReadNotification_SinAutenticacion_Retorna401()
        {
            HttpResponseMessage response = await _client.PatchAsync("/api/v1/notifications/readnotification/123/1", content: null);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task UpdateReadNotification_ConRolValido_Retorna204()
        {
            var request = AuthorizedRequest(HttpMethod.Patch, "/api/v1/notifications/readnotification/123/1", roles: "CLIENT");

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        private static HttpRequestMessage AuthorizedRequest(HttpMethod method, String url, String roles)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("X-Test-Auth", "true");
            request.Headers.Add("X-Test-Roles", roles);
            return request;
        }
    }
}
