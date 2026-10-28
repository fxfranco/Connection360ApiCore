using Connection360.Api.IntegrationTests.Infrastructure;
using Connection360.Application.DTOs;
using Connection360.Application.Ports;
using FluentAssertions;
using Moq;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Connection360.Api.IntegrationTests.Controllers
{
    public class MyShipmentsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public MyShipmentsControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.ResetMocks();
            _client = _factory.CreateClient();
        }

        private static HttpRequestMessage AuthorizedRequest(String url, String roles = "CLIENT")
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Test-Auth", "true");
            request.Headers.Add("X-Test-Roles", roles);
            return request;
        }

        private static MyShipmentsResponse SampleResponse(Int64 total = 5) => new()
        {
            ClientSummaryResponseData = new ClientSummaryResponse { TotalClientRecords = total }
        };

        [Fact]
        public async Task GetAllShipments_SinAutenticacion_Retorna401()
        {
            HttpResponseMessage response = await _client.GetAsync("/api/v1/myshipments/allshipments?idClient=123&page=1&size=10");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            _factory.MyShipmentsUseCaseMock.Verify(u => u.ExecuteGetAllShipmentsAsync(It.IsAny<MyShipmentsRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetAllShipments_ConRolNoPermitido_Retorna403()
        {
            var request = AuthorizedRequest("/api/v1/myshipments/allshipments?idClient=123&page=1&size=10", roles: "ROL_NO_PERMITIDO");

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Theory]
        [InlineData("ADMIN")]
        [InlineData("CLIENT")]
        [InlineData("ANALISTAOPE")]
        [InlineData("ANALISTASAC")]
        public async Task GetAllShipments_EnviaAlUseCaseElRolDelUsuarioAutenticado(String role)
        {
            _factory.MyShipmentsUseCaseMock
                .Setup(u => u.ExecuteGetAllShipmentsAsync(It.IsAny<MyShipmentsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(SampleResponse());

            var request = AuthorizedRequest("/api/v1/myshipments/allshipments?idClient=123&page=1&size=10", roles: role);

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            _factory.MyShipmentsUseCaseMock.Verify(u => u.ExecuteGetAllShipmentsAsync(
                It.Is<MyShipmentsRequest>(r => r.RoleName == role), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAllShipments_ConIdQueryClient_PropagaElClienteConsultadoYDesactivaAllClient()
        {
            _factory.MyShipmentsUseCaseMock
                .Setup(u => u.ExecuteGetAllShipmentsAsync(It.IsAny<MyShipmentsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(SampleResponse());

            var request = AuthorizedRequest("/api/v1/myshipments/allshipments?idClient=123&idQueryClient=CUS-10&page=1&size=10", roles: "ANALISTAOPE");

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            _factory.MyShipmentsUseCaseMock.Verify(u => u.ExecuteGetAllShipmentsAsync(
                It.Is<MyShipmentsRequest>(r => r.IdClient == "123" && !r.AllClient && r.IdQueryClient == "CUS-10" && r.Page == 1 && r.Size == 10),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetHistoryAllShipments_ConRolValido_Retorna200ConMetaDePaginacion()
        {
            _factory.MyShipmentsUseCaseMock
                .Setup(u => u.ExecuteGetHistoryAllShipmentsAsync(It.IsAny<MyShipmentsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(SampleResponse(9));

            var request = AuthorizedRequest("/api/v1/myshipments/allhistory?idClient=123&page=1&size=5");

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            json.RootElement.GetProperty("meta").GetProperty("totalItems").GetInt64().Should().Be(9);
        }

        [Fact]
        public async Task GetAllShipments_ConRolValido_Retorna200ConMetaDePaginacion()
        {
            _factory.MyShipmentsUseCaseMock
                .Setup(u => u.ExecuteGetAllShipmentsAsync(It.IsAny<MyShipmentsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(SampleResponse(42));

            var request = AuthorizedRequest("/api/v1/myshipments/allshipments?idClient=123&page=2&size=10");

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var meta = json.RootElement.GetProperty("meta");
            meta.GetProperty("totalItems").GetInt64().Should().Be(42);
            meta.GetProperty("currentPage").GetInt64().Should().Be(2);
            meta.GetProperty("limit").GetInt64().Should().Be(10);
        }

        [Fact]
        public async Task GetDetailsShipments_ConRolValido_Retorna200ConElResultado()
        {
            _factory.MyShipmentsUseCaseMock
                .Setup(u => u.ExecuteDetailsShipmentsAsync(It.IsAny<MyShipmentsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DetailsShipmentsResponse());

            var request = AuthorizedRequest("/api/v1/myshipments/detailsshipments?idClient=123&documentNumber=HBL-001");

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            _factory.MyShipmentsUseCaseMock.Verify(u => u.ExecuteDetailsShipmentsAsync(
                It.Is<MyShipmentsRequest>(r => r.IdClient == "123" && r.DocumentNumber == "HBL-001"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAllShipments_SiElUseCaseLanzaExcepcion_Retorna500ConElManejadorGlobal()
        {
            _factory.MyShipmentsUseCaseMock
                .Setup(u => u.ExecuteGetAllShipmentsAsync(It.IsAny<MyShipmentsRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("fallo simulado"));

            var request = AuthorizedRequest("/api/v1/myshipments/allshipments?idClient=123&page=1&size=10");

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            json.RootElement.GetProperty("message").GetString().Should().Be("Error interno del servidor");
        }
    }
}
