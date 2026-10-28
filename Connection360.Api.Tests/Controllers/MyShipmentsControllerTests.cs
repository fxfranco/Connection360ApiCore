using Connection360.Api.Controllers;
using Connection360.Api.Models;
using Connection360.Api.Tests.TestSupport;
using Connection360.Application.DTOs;
using Connection360.Application.Ports;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Connection360.Api.Tests.Controllers
{
    public class MyShipmentsControllerTests
    {
        private readonly Mock<IGetMyShipmentsUseCase> _useCaseMock = new();
        private readonly MyShipmentsController _sut;

        public MyShipmentsControllerTests()
        {
            _sut = new MyShipmentsController(_useCaseMock.Object)
            {
                ControllerContext = ControllerContextFactory.Create("CLIENT")
            };
        }

        private static MyShipmentsResponse SampleResponse(Int64 total = 10) => new()
        {
            ClientSummaryResponseData = new ClientSummaryResponse { TotalClientRecords = total }
        };

        [Fact]
        public async Task GetAllShipments_RetornaOkConPagedResultCorrectamenteArmado()
        {
            _useCaseMock.Setup(u => u.ExecuteGetAllShipmentsAsync(It.IsAny<MyShipmentsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(SampleResponse(50));

            IActionResult result = await _sut.GetAllShipments("123", null, 2, 10, CancellationToken.None);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var paged = okResult.Value.Should().BeOfType<PagedResult<Object>>().Subject;
            paged.TotalItems.Should().Be(50);
            paged.CurrentPage.Should().Be(2);
            paged.Limit.Should().Be(10);
            paged.Items.Should().ContainSingle();
        }

        [Fact]
        public async Task GetAllShipments_PropagaIdClientPageYSizeAlUseCase()
        {
            _useCaseMock.Setup(u => u.ExecuteGetAllShipmentsAsync(It.IsAny<MyShipmentsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(SampleResponse());

            await _sut.GetAllShipments("CLIENTE-1", null, 3, 20, CancellationToken.None);

            _useCaseMock.Verify(u => u.ExecuteGetAllShipmentsAsync(
                It.Is<MyShipmentsRequest>(r => r.IdClient == "CLIENTE-1" && r.Page == 3 && r.Size == 20),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task GetAllShipments_SinIdQueryClient_MarcaAllClientYDejaIdQueryClientVacio(String? idQueryClient)
        {
            _useCaseMock.Setup(u => u.ExecuteGetAllShipmentsAsync(It.IsAny<MyShipmentsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(SampleResponse());

            await _sut.GetAllShipments("123", idQueryClient, 1, 10, CancellationToken.None);

            _useCaseMock.Verify(u => u.ExecuteGetAllShipmentsAsync(
                It.Is<MyShipmentsRequest>(r => r.AllClient && r.IdQueryClient == String.Empty),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAllShipments_ConIdQueryClient_DesactivaAllClient()
        {
            _useCaseMock.Setup(u => u.ExecuteGetAllShipmentsAsync(It.IsAny<MyShipmentsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(SampleResponse());

            await _sut.GetAllShipments("123", "CUS-10", 1, 10, CancellationToken.None);

            _useCaseMock.Verify(u => u.ExecuteGetAllShipmentsAsync(
                It.Is<MyShipmentsRequest>(r => !r.AllClient && r.IdQueryClient == "CUS-10"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("ADMIN")]
        [InlineData("CLIENT")]
        [InlineData("ANALISTAOPE")]
        [InlineData("ANALISTASAC")]
        public async Task GetAllShipments_EnviaElRolDelUsuarioAutenticado(String role)
        {
            _sut.ControllerContext = ControllerContextFactory.Create(role);
            _useCaseMock.Setup(u => u.ExecuteGetAllShipmentsAsync(It.IsAny<MyShipmentsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(SampleResponse());

            await _sut.GetAllShipments("123", null, 1, 10, CancellationToken.None);

            _useCaseMock.Verify(u => u.ExecuteGetAllShipmentsAsync(It.Is<MyShipmentsRequest>(r => r.RoleName == role), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task FilterShipments_RetornaOkConPagedResultYPropagaLosParametrosBase()
        {
            _useCaseMock.Setup(u => u.ExecuteFilterShipmentsAsync(It.IsAny<MyShipmentsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(SampleResponse(12));
            var filters = new MyShipmentsFiltersRequest { OperationType = "IMPO" };

            IActionResult result = await _sut.FilterShipments("123", "CUS-10", 4, 25, filters, CancellationToken.None);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var paged = okResult.Value.Should().BeOfType<PagedResult<Object>>().Subject;
            paged.TotalItems.Should().Be(12);
            paged.CurrentPage.Should().Be(4);
            paged.Limit.Should().Be(25);

            _useCaseMock.Verify(u => u.ExecuteFilterShipmentsAsync(
                It.Is<MyShipmentsRequest>(r => r.IdClient == "123" && r.IdQueryClient == "CUS-10" && !r.AllClient && r.Page == 4 && r.Size == 25 && r.Filters == filters),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetHistoryAllShipments_RetornaOkConPagedResult()
        {
            _useCaseMock.Setup(u => u.ExecuteGetHistoryAllShipmentsAsync(It.IsAny<MyShipmentsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(SampleResponse(30));

            IActionResult result = await _sut.GetHistoryAllShipments("123", null, 1, 10, CancellationToken.None);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var paged = okResult.Value.Should().BeOfType<PagedResult<Object>>().Subject;
            paged.TotalItems.Should().Be(30);
        }

        [Fact]
        public async Task GetHistoryAllShipments_PropagaLosParametrosAlUseCase()
        {
            _useCaseMock.Setup(u => u.ExecuteGetHistoryAllShipmentsAsync(It.IsAny<MyShipmentsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(SampleResponse());

            await _sut.GetHistoryAllShipments("123", "CUS-10", 2, 5, CancellationToken.None);

            _useCaseMock.Verify(u => u.ExecuteGetHistoryAllShipmentsAsync(
                It.Is<MyShipmentsRequest>(r => r.IdClient == "123" && r.IdQueryClient == "CUS-10" && !r.AllClient && r.Page == 2 && r.Size == 5 && r.RoleName == "CLIENT"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task FilterHistoryShipments_RetornaOkConPagedResult()
        {
            _useCaseMock.Setup(u => u.ExecuteFilterHistoryShipmentsAsync(It.IsAny<MyShipmentsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(SampleResponse(15));
            var filters = new MyShipmentsFiltersRequest();

            IActionResult result = await _sut.FilterHistoryShipments("123", null, 1, 10, filters, CancellationToken.None);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var paged = okResult.Value.Should().BeOfType<PagedResult<Object>>().Subject;
            paged.TotalItems.Should().Be(15);
        }

        [Fact]
        public async Task FilterHistoryShipments_PropagaLosFiltrosAlUseCase()
        {
            _useCaseMock.Setup(u => u.ExecuteFilterHistoryShipmentsAsync(It.IsAny<MyShipmentsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(SampleResponse());
            var filters = new MyShipmentsFiltersRequest { OperationType = "EXPO" };

            await _sut.FilterHistoryShipments("123", "CUS-10", 1, 10, filters, CancellationToken.None);

            _useCaseMock.Verify(u => u.ExecuteFilterHistoryShipmentsAsync(
                It.Is<MyShipmentsRequest>(r => r.Filters == filters && r.IdQueryClient == "CUS-10" && !r.AllClient),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetDetailsShipments_RetornaOkConElResultadoDelUseCase()
        {
            var expected = new DetailsShipmentsResponse();
            _useCaseMock.Setup(u => u.ExecuteDetailsShipmentsAsync(It.IsAny<MyShipmentsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(expected);

            IActionResult result = await _sut.GetDetailsShipments("123", null, "HBL-001", CancellationToken.None);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeSameAs(expected);
        }

        [Fact]
        public async Task GetDetailsShipments_PropagaIdClientYDocumentNumber()
        {
            _useCaseMock.Setup(u => u.ExecuteDetailsShipmentsAsync(It.IsAny<MyShipmentsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new DetailsShipmentsResponse());

            await _sut.GetDetailsShipments("CLIENTE-1", null, "HBL-999", CancellationToken.None);

            _useCaseMock.Verify(u => u.ExecuteDetailsShipmentsAsync(
                It.Is<MyShipmentsRequest>(r => r.IdClient == "CLIENTE-1" && r.DocumentNumber == "HBL-999" && r.AllClient && r.IdQueryClient == String.Empty && r.RoleName == "CLIENT"),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
