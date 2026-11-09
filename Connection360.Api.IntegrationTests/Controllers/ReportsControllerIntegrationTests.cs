using Connection360.Api.IntegrationTests.Infrastructure;
using Connection360.Application.DTOs;
using FluentAssertions;
using Moq;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Connection360.Api.IntegrationTests.Controllers
{
    public class ReportsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ReportsControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.ResetMocks();
            _client = _factory.CreateClient();
        }

        private static HttpRequestMessage AuthorizedRequest(String url, String roles)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Test-Auth", "true");
            request.Headers.Add("X-Test-Roles", roles);
            return request;
        }

        [Fact]
        public async Task GetReportTotals_SinAutenticacion_Retorna401()
        {
            // ReportsController usa [Authorize] a nivel de clase (ya no es publico).
            HttpResponseMessage response = await _client.GetAsync("/api/v1/reports/home?idClient=123");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetReportTotals_ConRolNoPermitido_Retorna403()
        {
            var request = AuthorizedRequest("/api/v1/reports/home?idClient=123", roles: "ROL_NO_PERMITIDO");

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetReportTotals_ConRolValido_RetornaLaListaEnvueltaConLosDatosDelUseCase()
        {
            _factory.ReportsUseCaseMock
                .Setup(u => u.ExecuteGetReportsTotalsAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ReportsSummaryResponse>
                {
                    new() { ClientNit = "111", TotalClientRecords = 15, TotalInvoiced = 999.5 },
                    new() { ClientNit = "222", TotalClientRecords = 4 }
                });

            var request = AuthorizedRequest("/api/v1/reports/home?idClient=123", roles: "CLIENT");

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            JsonElement data = json.RootElement.GetProperty("dataResponse");
            data.GetArrayLength().Should().Be(2);
            data[0].GetProperty("totalClientRecords").GetInt64().Should().Be(15);
            data[1].GetProperty("clientNit").GetString().Should().Be("222");
        }

        [Fact]
        public async Task GetReportTotals_PropagaElRolDelUsuarioYElClienteConsultado()
        {
            _factory.ReportsUseCaseMock
                .Setup(u => u.ExecuteGetReportsTotalsAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ReportsSummaryResponse>());

            var request = AuthorizedRequest("/api/v1/reports/home?idClient=123&idQueryClient=CUS-10", roles: "ANALISTASAC");

            HttpResponseMessage response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            _factory.ReportsUseCaseMock.Verify(u => u.ExecuteGetReportsTotalsAsync(
                It.Is<ClientSummaryRequest>(r => r.IdClient == "123" && r.RoleName == "ANALISTASAC" && !r.AllClient && r.IdQueryClient == "CUS-10"),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
