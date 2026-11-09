using Connection360.Api.Controllers;
using Connection360.Api.Models;
using Connection360.Api.Tests.TestSupport;
using Connection360.Application.DTOs;
using Connection360.Application.Ports;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Connection360.Api.Tests.Controllers
{
    /// <summary>
    /// Rutas 403 (usuario sin rol asignado) y composicion de la respuesta de los controladores
    /// Home, MyShipments y Reports.
    /// </summary>
    public class ControllersForbiddenTests
    {
        private const String MensajeDenegado = "Acceso denegado. No se tiene un rol asignado.";

        private static String[] Split(String csv) => csv.Split(',', StringSplitOptions.RemoveEmptyEntries);

        private static void AssertForbidden(IActionResult result)
        {
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
            objectResult.Value.Should().Be(MensajeDenegado);
        }

        private static MyShipmentsResponse SampleResponse(Int64 total = 10) => new()
        {
            ClientSummaryResponseData = new ClientSummaryResponse { TotalClientRecords = total }
        };

        // ---------- Home ----------

        [Theory]
        [InlineData("")]
        [InlineData("ROL_DESCONOCIDO")]
        public async Task HomeController_SinRolAsignado_GetHomeTotalsRetorna403(String rolesCsv)
        {
            var useCase = new Mock<IGetClientSummaryUseCase>();
            var sut = new HomeController(useCase.Object) { ControllerContext = ControllerContextFactory.Create(Split(rolesCsv)) };

            IActionResult result = await sut.GetHomeTotals("123", null, CancellationToken.None);

            AssertForbidden(result);
            useCase.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("")]
        [InlineData("ROL_DESCONOCIDO")]
        public async Task HomeController_SinRolAsignado_GetHomeFiltersRetorna403(String rolesCsv)
        {
            var useCase = new Mock<IGetClientSummaryUseCase>();
            var sut = new HomeController(useCase.Object) { ControllerContext = ControllerContextFactory.Create(Split(rolesCsv)) };

            IActionResult result = await sut.GetHomeFilters("123", null, "HBL", CancellationToken.None);

            AssertForbidden(result);
            useCase.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task HomeController_ConVariosRoles_EnviaElPrimeroDelEnum()
        {
            var useCase = new Mock<IGetClientSummaryUseCase>();
            useCase.Setup(u => u.ExecuteTotalsAsync(It.IsAny<ClientSummaryRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ClientSummaryResponse());
            var sut = new HomeController(useCase.Object) { ControllerContext = ControllerContextFactory.Create("ANALISTASAC", "ADMIN") };

            await sut.GetHomeTotals("123", null, CancellationToken.None);

            useCase.Verify(u => u.ExecuteTotalsAsync(It.Is<ClientSummaryRequest>(r => r.RoleName == "ADMIN"), It.IsAny<CancellationToken>()), Times.Once);
        }

        // ---------- Reports ----------

        [Fact]
        public async Task ReportsController_SinRolAsignado_Retorna403()
        {
            var useCase = new Mock<IGetReportsUseCase>();
            var sut = new ReportsController(useCase.Object) { ControllerContext = ControllerContextFactory.Create() };

            IActionResult result = await sut.GetReportTotals("123", null, CancellationToken.None);

            AssertForbidden(result);
            useCase.VerifyNoOtherCalls();
        }

        // ---------- MyShipments ----------

        private static (MyShipmentsController Sut, Mock<IGetMyShipmentsUseCase> UseCase) BuildMyShipments(params String[] roles)
        {
            var useCase = new Mock<IGetMyShipmentsUseCase>();
            var sut = new MyShipmentsController(useCase.Object) { ControllerContext = ControllerContextFactory.Create(roles) };
            return (sut, useCase);
        }

        [Fact]
        public async Task MyShipmentsController_SinRolAsignado_GetAllShipmentsRetorna403()
        {
            var (sut, useCase) = BuildMyShipments();

            AssertForbidden(await sut.GetAllShipments("123", null, 0, 10, CancellationToken.None));
            useCase.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task MyShipmentsController_SinRolAsignado_FilterShipmentsRetorna403()
        {
            var (sut, useCase) = BuildMyShipments();

            AssertForbidden(await sut.FilterShipments("123", null, 0, 10, new MyShipmentsFiltersRequest(), CancellationToken.None));
            useCase.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task MyShipmentsController_SinRolAsignado_GetHistoryAllShipmentsRetorna403()
        {
            var (sut, useCase) = BuildMyShipments();

            AssertForbidden(await sut.GetHistoryAllShipments("123", null, 0, 10, CancellationToken.None));
            useCase.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task MyShipmentsController_SinRolAsignado_FilterHistoryShipmentsRetorna403()
        {
            var (sut, useCase) = BuildMyShipments("OTRO");

            AssertForbidden(await sut.FilterHistoryShipments("123", null, 0, 10, new MyShipmentsFiltersRequest(), CancellationToken.None));
            useCase.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task MyShipmentsController_SinRolAsignado_GetDetailsShipmentsRetorna403()
        {
            var (sut, useCase) = BuildMyShipments();

            AssertForbidden(await sut.GetDetailsShipments("123", null, "DOC-1", CancellationToken.None));
            useCase.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("ADMIN")]
        [InlineData("CLIENT")]
        [InlineData("ANALISTAOPE")]
        [InlineData("ANALISTASAC")]
        public async Task MyShipmentsController_EnviaElRolEnTodosLosEndpoints(String role)
        {
            var (sut, useCase) = BuildMyShipments(role);
            useCase.Setup(u => u.ExecuteGetAllShipmentsAsync(It.IsAny<MyShipmentsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(SampleResponse());
            useCase.Setup(u => u.ExecuteFilterShipmentsAsync(It.IsAny<MyShipmentsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(SampleResponse());
            useCase.Setup(u => u.ExecuteGetHistoryAllShipmentsAsync(It.IsAny<MyShipmentsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(SampleResponse());
            useCase.Setup(u => u.ExecuteFilterHistoryShipmentsAsync(It.IsAny<MyShipmentsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(SampleResponse());
            useCase.Setup(u => u.ExecuteDetailsShipmentsAsync(It.IsAny<MyShipmentsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new DetailsShipmentsResponse());

            await sut.GetAllShipments("1", null, 0, 5, CancellationToken.None);
            await sut.FilterShipments("1", null, 0, 5, new MyShipmentsFiltersRequest(), CancellationToken.None);
            await sut.GetHistoryAllShipments("1", null, 0, 5, CancellationToken.None);
            await sut.FilterHistoryShipments("1", null, 0, 5, new MyShipmentsFiltersRequest(), CancellationToken.None);
            await sut.GetDetailsShipments("1", null, "DOC", CancellationToken.None);

            useCase.Invocations.Select(i => i.Arguments[0]).Cast<MyShipmentsRequest>().Should().OnlyContain(r => r.RoleName == role).And.HaveCount(5);
        }

        [Fact]
        public async Task GetDetailsShipments_ConIdQueryClient_DesactivaAllClientYPropagaDocumentNumber()
        {
            var (sut, useCase) = BuildMyShipments("CLIENT");
            useCase.Setup(u => u.ExecuteDetailsShipmentsAsync(It.IsAny<MyShipmentsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new DetailsShipmentsResponse());

            await sut.GetDetailsShipments("1", "CUS-5", "DOC-7", CancellationToken.None);

            useCase.Verify(u => u.ExecuteDetailsShipmentsAsync(
                It.Is<MyShipmentsRequest>(r => !r.AllClient && r.IdQueryClient == "CUS-5" && r.DocumentNumber == "DOC-7"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task FilterShipments_ConIdQueryClient_PropagaFiltrosYDesactivaAllClient()
        {
            var (sut, useCase) = BuildMyShipments("CLIENT");
            var filters = new MyShipmentsFiltersRequest();
            useCase.Setup(u => u.ExecuteFilterShipmentsAsync(It.IsAny<MyShipmentsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(SampleResponse(0));

            IActionResult result = await sut.FilterShipments("1", "CUS-5", 1, 7, filters, CancellationToken.None);

            useCase.Verify(u => u.ExecuteFilterShipmentsAsync(
                It.Is<MyShipmentsRequest>(r => !r.AllClient && r.IdQueryClient == "CUS-5" && ReferenceEquals(r.Filters, filters)),
                It.IsAny<CancellationToken>()), Times.Once);
            result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<PagedResult<Object>>().Which.TotalPages.Should().Be(0);
        }

        [Fact]
        public async Task GetHistoryAllShipments_ConIdQueryClient_DesactivaAllClient()
        {
            var (sut, useCase) = BuildMyShipments("CLIENT");
            useCase.Setup(u => u.ExecuteGetHistoryAllShipmentsAsync(It.IsAny<MyShipmentsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(SampleResponse());

            await sut.GetHistoryAllShipments("1", "CUS-5", 0, 5, CancellationToken.None);

            useCase.Verify(u => u.ExecuteGetHistoryAllShipmentsAsync(
                It.Is<MyShipmentsRequest>(r => !r.AllClient && r.IdQueryClient == "CUS-5"), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task FilterHistoryShipments_ConIdQueryClient_DesactivaAllClient()
        {
            var (sut, useCase) = BuildMyShipments("CLIENT");
            useCase.Setup(u => u.ExecuteFilterHistoryShipmentsAsync(It.IsAny<MyShipmentsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(SampleResponse());

            await sut.FilterHistoryShipments("1", "CUS-5", 0, 5, new MyShipmentsFiltersRequest(), CancellationToken.None);

            useCase.Verify(u => u.ExecuteFilterHistoryShipmentsAsync(
                It.Is<MyShipmentsRequest>(r => !r.AllClient && r.IdQueryClient == "CUS-5"), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAllShipments_ConTotalYLimite_CalculaTotalPages()
        {
            var (sut, useCase) = BuildMyShipments("CLIENT");
            useCase.Setup(u => u.ExecuteGetAllShipmentsAsync(It.IsAny<MyShipmentsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(SampleResponse(25));

            IActionResult result = await sut.GetAllShipments("1", null, 0, 10, CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<PagedResult<Object>>().Which.TotalPages.Should().Be(3);
        }
    }
}
