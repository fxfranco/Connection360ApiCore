using Connection360.Api.Controllers;
using Connection360.Api.Tests.TestSupport;
using Connection360.Application.DTOs;
using Connection360.Application.Ports;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Connection360.Api.Tests.Controllers
{
    public class ReportsControllerTests
    {
        private readonly Mock<IGetReportsUseCase> _useCaseMock = new();
        private readonly ReportsController _sut;

        public ReportsControllerTests()
        {
            _sut = new ReportsController(_useCaseMock.Object)
            {
                ControllerContext = ControllerContextFactory.Create("CLIENT")
            };
        }

        [Fact]
        public async Task GetReportTotals_RetornaOkConElResultadoDelUseCase()
        {
            var expected = new List<ReportsSummaryResponse> { new() { TotalClientRecords = 8 } };
            _useCaseMock.Setup(u => u.ExecuteGetReportsTotalsAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(expected);

            IActionResult result = await _sut.GetReportTotals("123", null, CancellationToken.None);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeSameAs(expected);
        }

        [Fact]
        public async Task GetReportTotals_PropagaElIdClientAlUseCase()
        {
            _useCaseMock.Setup(u => u.ExecuteGetReportsTotalsAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<ReportsSummaryResponse>());

            await _sut.GetReportTotals("CLIENTE-77", null, CancellationToken.None);

            _useCaseMock.Verify(u => u.ExecuteGetReportsTotalsAsync(It.Is<ClientSummaryRequest>(r => r.IdClient == "CLIENTE-77"), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task GetReportTotals_SinIdQueryClient_MarcaAllClient(String? idQueryClient)
        {
            _useCaseMock.Setup(u => u.ExecuteGetReportsTotalsAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<ReportsSummaryResponse>());

            await _sut.GetReportTotals("123", idQueryClient, CancellationToken.None);

            _useCaseMock.Verify(u => u.ExecuteGetReportsTotalsAsync(
                It.Is<ClientSummaryRequest>(r => r.AllClient && r.IdQueryClient == String.Empty),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetReportTotals_ConIdQueryClient_DesactivaAllClient()
        {
            _useCaseMock.Setup(u => u.ExecuteGetReportsTotalsAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<ReportsSummaryResponse>());

            await _sut.GetReportTotals("123", "CUS-10", CancellationToken.None);

            _useCaseMock.Verify(u => u.ExecuteGetReportsTotalsAsync(
                It.Is<ClientSummaryRequest>(r => !r.AllClient && r.IdQueryClient == "CUS-10"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("ADMIN")]
        [InlineData("CLIENT")]
        [InlineData("ANALISTAOPE")]
        [InlineData("ANALISTASAC")]
        public async Task GetReportTotals_EnviaElRolDelUsuarioAutenticado(String role)
        {
            _sut.ControllerContext = ControllerContextFactory.Create(role);
            _useCaseMock.Setup(u => u.ExecuteGetReportsTotalsAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<ReportsSummaryResponse>());

            await _sut.GetReportTotals("123", null, CancellationToken.None);

            _useCaseMock.Verify(u => u.ExecuteGetReportsTotalsAsync(It.Is<ClientSummaryRequest>(r => r.RoleName == role), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
