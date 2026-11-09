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
    public class HomeControllerTests
    {
        private readonly Mock<IGetClientSummaryUseCase> _useCaseMock = new();
        private readonly HomeController _sut;

        public HomeControllerTests()
        {
            _sut = new HomeController(_useCaseMock.Object)
            {
                ControllerContext = ControllerContextFactory.Create("CLIENT")
            };
        }

        [Fact]
        public async Task GetHomeTotals_RetornaOkConElResultadoDelUseCase()
        {
            var expected = new ClientSummaryResponse { TotalClientRecords = 5 };
            _useCaseMock.Setup(u => u.ExecuteTotalsAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(expected);

            IActionResult result = await _sut.GetHomeTotals("123", null, CancellationToken.None);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeSameAs(expected);
        }

        [Fact]
        public async Task GetHomeTotals_PropagaElIdClientAlUseCase()
        {
            _useCaseMock.Setup(u => u.ExecuteTotalsAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ClientSummaryResponse());

            await _sut.GetHomeTotals("CLIENTE-999", null, CancellationToken.None);

            _useCaseMock.Verify(u => u.ExecuteTotalsAsync(It.Is<ClientSummaryRequest>(r => r.IdClient == "CLIENTE-999"), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task GetHomeTotals_SinIdQueryClient_MarcaAllClientYDejaIdQueryClientVacio(String? idQueryClient)
        {
            _useCaseMock.Setup(u => u.ExecuteTotalsAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ClientSummaryResponse());

            await _sut.GetHomeTotals("123", idQueryClient, CancellationToken.None);

            _useCaseMock.Verify(u => u.ExecuteTotalsAsync(
                It.Is<ClientSummaryRequest>(r => r.AllClient && r.IdQueryClient == String.Empty),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetHomeTotals_ConIdQueryClient_DesactivaAllClientYPropagaElCliente()
        {
            _useCaseMock.Setup(u => u.ExecuteTotalsAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ClientSummaryResponse());

            await _sut.GetHomeTotals("123", "CUS-10", CancellationToken.None);

            _useCaseMock.Verify(u => u.ExecuteTotalsAsync(
                It.Is<ClientSummaryRequest>(r => !r.AllClient && r.IdQueryClient == "CUS-10"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("ADMIN")]
        [InlineData("CLIENT")]
        [InlineData("ANALISTAOPE")]
        [InlineData("ANALISTASAC")]
        public async Task GetHomeTotals_EnviaElRolDelUsuarioAutenticado(String role)
        {
            _sut.ControllerContext = ControllerContextFactory.Create(role);
            _useCaseMock.Setup(u => u.ExecuteTotalsAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ClientSummaryResponse());

            await _sut.GetHomeTotals("123", null, CancellationToken.None);

            _useCaseMock.Verify(u => u.ExecuteTotalsAsync(It.Is<ClientSummaryRequest>(r => r.RoleName == role), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetHomeFilters_RetornaOkConElResultadoDelUseCase()
        {
            var expected = new ResumenClienteResponse { DocumentNumber = "HBL-001" };
            _useCaseMock.Setup(u => u.ExecuteFilterAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(expected);

            IActionResult result = await _sut.GetHomeFilters("123", null, "HBL-001", CancellationToken.None);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeSameAs(expected);
        }

        [Fact]
        public async Task GetHomeFilters_PropagaElFilterValueAlUseCase()
        {
            _useCaseMock.Setup(u => u.ExecuteFilterAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ResumenClienteResponse());

            await _sut.GetHomeFilters("123", null, "HBL-001", CancellationToken.None);

            _useCaseMock.Verify(u => u.ExecuteFilterAsync(
                It.Is<ClientSummaryRequest>(r => r.IdClient == "123" && r.FilterValue == "HBL-001" && r.AllClient && r.IdQueryClient == String.Empty),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("ADMIN")]
        [InlineData("CLIENT")]
        [InlineData("ANALISTAOPE")]
        [InlineData("ANALISTASAC")]
        public async Task GetHomeFilters_EnviaElRolDelUsuarioAutenticado(String role)
        {
            _sut.ControllerContext = ControllerContextFactory.Create(role);
            _useCaseMock.Setup(u => u.ExecuteFilterAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ResumenClienteResponse());

            await _sut.GetHomeFilters("123", null, "HBL-001", CancellationToken.None);

            _useCaseMock.Verify(u => u.ExecuteFilterAsync(It.Is<ClientSummaryRequest>(r => r.RoleName == role), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetHomeFilters_ConIdQueryClient_DesactivaAllClient()
        {
            _useCaseMock.Setup(u => u.ExecuteFilterAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ResumenClienteResponse());

            await _sut.GetHomeFilters("123", "CUS-10", "HBL-001", CancellationToken.None);

            _useCaseMock.Verify(u => u.ExecuteFilterAsync(
                It.Is<ClientSummaryRequest>(r => !r.AllClient && r.IdQueryClient == "CUS-10" && r.RoleName == "CLIENT"),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
