using Connection360.Api.IntegrationTests.Infrastructure;
using Connection360.Application.DTOs;
using FluentAssertions;
using Moq;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Connection360.Api.IntegrationTests.Controllers
{
    public class HomeControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public HomeControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.ResetMocks();
            _client = _factory.CreateClient();
        }

        private static HttpRequestMessage AuthorizedRequest(HttpMethod method, String url, String roles = "CLIENT")
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("X-Test-Auth", "true");
            request.Headers.Add("X-Test-Roles", roles);
            return request;
        }

        [Fact]
        public async Task GetHomeTotals_SinAutenticacion_Retorna401()
        {
            HttpResponseMessage response = await _client.GetAsync("/api/v1/home/totals?idClient=123");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            _factory.ClientSummaryUseCaseMock.Verify(u => u.ExecuteTotalsAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetHomeTotals_ConRolNoPermitido_Retorna403()
        {
            var request = AuthorizedRequest(HttpMethod.Get, "/api/v1/home/totals?idClient=123", roles: "ROL_NO_PERMITIDO");

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Theory]
        [InlineData("ADMIN")]
        [InlineData("CLIENT")]
        [InlineData("ANALISTAOPE")]
        [InlineData("ANALISTASAC")]
        public async Task GetHomeTotals_EnviaAlUseCaseElRolDelUsuarioAutenticado(String role)
        {
            _factory.ClientSummaryUseCaseMock
                .Setup(u => u.ExecuteTotalsAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ClientSummaryResponse());

            var request = AuthorizedRequest(HttpMethod.Get, "/api/v1/home/totals?idClient=123", roles: role);

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            _factory.ClientSummaryUseCaseMock.Verify(u => u.ExecuteTotalsAsync(
                It.Is<ClientSummaryRequest>(r => r.RoleName == role), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetHomeTotals_ConRolValido_Retorna200ConLaRespuestaEnvueltaPorElFiltro()
        {
            _factory.ClientSummaryUseCaseMock
                .Setup(u => u.ExecuteTotalsAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ClientSummaryResponse { TotalClientRecords = 7, TotalImports = 3 });

            var request = AuthorizedRequest(HttpMethod.Get, "/api/v1/home/totals?idClient=123", roles: "CLIENT");

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            json.RootElement.GetProperty("status").GetInt32().Should().Be(200);
            json.RootElement.GetProperty("message").GetString().Should().Be("Solicitud exitosa");
            json.RootElement.GetProperty("dataResponse").GetProperty("totalClientRecords").GetInt64().Should().Be(7);
        }

        [Fact]
        public async Task GetHomeTotals_SinIdQueryClient_PropagaAllClientAlUseCase()
        {
            _factory.ClientSummaryUseCaseMock
                .Setup(u => u.ExecuteTotalsAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ClientSummaryResponse());

            var request = AuthorizedRequest(HttpMethod.Get, "/api/v1/home/totals?idClient=123", roles: "ANALISTAOPE");

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            _factory.ClientSummaryUseCaseMock.Verify(u => u.ExecuteTotalsAsync(
                It.Is<ClientSummaryRequest>(r => r.IdClient == "123" && r.AllClient && r.IdQueryClient == String.Empty),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetHomeTotals_ConIdQueryClient_PropagaElClienteConsultadoYDesactivaAllClient()
        {
            _factory.ClientSummaryUseCaseMock
                .Setup(u => u.ExecuteTotalsAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ClientSummaryResponse());

            var request = AuthorizedRequest(HttpMethod.Get, "/api/v1/home/totals?idClient=123&idQueryClient=CUS-10", roles: "ANALISTASAC");

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            _factory.ClientSummaryUseCaseMock.Verify(u => u.ExecuteTotalsAsync(
                It.Is<ClientSummaryRequest>(r => r.IdClient == "123" && !r.AllClient && r.IdQueryClient == "CUS-10"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetHomeTotals_SinIdClient_Retorna400PorValidacionDeModelo()
        {
            var request = AuthorizedRequest(HttpMethod.Get, "/api/v1/home/totals", roles: "CLIENT");

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetHomeTotals_SiElResolverRechazaElAcceso_Retorna400ConElManejadorGlobal()
        {
            _factory.ClientSummaryUseCaseMock
                .Setup(u => u.ExecuteTotalsAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("No hay clientes asignados."));

            var request = AuthorizedRequest(HttpMethod.Get, "/api/v1/home/totals?idClient=123", roles: "CLIENT");

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            json.RootElement.GetProperty("error").GetString().Should().Be("No hay clientes asignados.");
        }

        [Fact]
        public async Task GetHomeFilters_ConRolValido_Retorna200YPropagaElFilterValue()
        {
            _factory.ClientSummaryUseCaseMock
                .Setup(u => u.ExecuteFilterAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResumenClienteResponse { DocumentNumber = "HBL-001" });

            var request = AuthorizedRequest(HttpMethod.Get, "/api/v1/home/filters?idClient=123&filterValue=HBL-001", roles: "ADMIN");

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            _factory.ClientSummaryUseCaseMock.Verify(u => u.ExecuteFilterAsync(
                It.Is<ClientSummaryRequest>(r => r.IdClient == "123" && r.FilterValue == "HBL-001"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RutaInexistente_Retorna404ConElCuerpoEstandarDeNotFoundMiddleware()
        {
            HttpResponseMessage response = await _client.GetAsync("/api/v1/ruta-que-no-existe");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            json.RootElement.GetProperty("message").GetString().Should().Be("Ruta no encontrada");
        }
    }
}
