using Connection360Notification.Api.IntegrationTests.Infrastructure;
using FluentAssertions;
using Moq;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Connection360Notification.Api.IntegrationTests.Hosting
{
    /// <summary>
    /// Rama "no Development" de Program.cs: sin Swagger, con HSTS y UseExceptionHandler("/error").
    /// </summary>
    public class ProductionEnvironmentIntegrationTests : IClassFixture<ProductionWebApplicationFactory>
    {
        private readonly ProductionWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ProductionEnvironmentIntegrationTests(ProductionWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.ResetMocks();
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task Swagger_EnProduccion_NoEstaExpuesto()
        {
            HttpResponseMessage response = await _client.GetAsync("/swagger/v1/swagger.json");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task SwaggerUI_EnProduccion_NoEstaExpuesto()
        {
            HttpResponseMessage response = await _client.GetAsync("/swagger/index.html");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Health_EnProduccion_Retorna200()
        {
            HttpResponseMessage response = await _client.GetAsync("/health");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Error_EnProduccion_RetornaProblemDetailsSinDetallesInternos()
        {
            HttpResponseMessage response = await _client.GetAsync("/error");

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            doc.RootElement.GetProperty("title").GetString().Should().Be("Ha ocurrido un error inesperado.");
        }

        [Fact]
        public async Task ExcepcionNoControlada_EnProduccion_LaCapturaElMiddlewareDeExcepciones()
        {
            _factory.NotificationsUseCaseMock
                .Setup(u => u.ExecuteGetNotificationsAllAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("detalle"));

            HttpResponseMessage response = await _client.GetAsync("/api/v1/notifications/allnotificationslistTest");

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            doc.RootElement.GetProperty("message").GetString().Should().Be("Error interno del servidor");
        }

        [Fact]
        public async Task RutaInexistente_EnProduccion_Retorna404ConElEnvoltorioEstandar()
        {
            HttpResponseMessage response = await _client.GetAsync("/api/v1/no-existe");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            doc.RootElement.GetProperty("message").GetString().Should().Be("Ruta no encontrada");
        }

        [Fact]
        public async Task GetAllNotifications_EnProduccion_SinAutenticacion_Retorna401()
        {
            HttpResponseMessage response = await _client.GetAsync("/api/v1/notifications/allnotifications?idClient=1");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
