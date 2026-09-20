using Connection360.Application.DTOs;
using Connection360.Application.Ports;
using Connection360.Application.UseCases;
using Connection360.Domain.Dtos;
using Connection360.Domain.Entities;
using Connection360.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;
using static Connection360.Application.Tests.TestSupport.DynamicDataSetBuilder;

namespace Connection360.Application.Tests.UseCases
{
    public class GetReportsUseCaseTests
    {
        private readonly Mock<IExternalDataGateway> _externalDataGatewayMock = new();
        private readonly Mock<IReportsDomainService> _reportsDomainServiceMock = new();
        private readonly Mock<IClientAccessResolver> _clientAccessResolverMock = new();
        private readonly List<CustomersOfCollaboratorDtoResult> _customersOfCollaborator = new()
        {
            new() { IdCollaborator = 1, IdentificacionCollaborator = "COL-1", IdCustomer = 10, IdentificacionCustomer = "CUS-10" }
        };
        private readonly GetReportsUseCase _sut;

        public GetReportsUseCaseTests()
        {
            _sut = new GetReportsUseCase(_externalDataGatewayMock.Object, _reportsDomainServiceMock.Object, _clientAccessResolverMock.Object);

            _clientAccessResolverMock
                .Setup(r => r.ResolveAsync(It.IsAny<ResolveClientAccessRequest>()))
                .ReturnsAsync(_customersOfCollaborator);

            _externalDataGatewayMock
                .Setup(g => g.FetchDataAsync(It.IsAny<String>(), It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Empty());
        }

        [Fact]
        public async Task ExecuteGetReportsTotalsAsync_SiElResolverRechazaElAcceso_PropagaLaExcepcionYNoConsultaElGateway()
        {
            var request = new ClientSummaryRequest { IdClient = "" };
            _clientAccessResolverMock
                .Setup(r => r.ResolveAsync(It.IsAny<ResolveClientAccessRequest>()))
                .ThrowsAsync(new ArgumentException("El campo 'cliente' es obligatorio."));

            Func<Task> act = () => _sut.ExecuteGetReportsTotalsAsync(request, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
            _externalDataGatewayMock.Verify(g => g.FetchDataAsync(It.IsAny<String>(), It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteGetReportsTotalsAsync_ResuelveElAccesoConLosDatosDeLaSolicitud()
        {
            var request = new ClientSummaryRequest { IdClient = "123", RoleName = "ANALISTASAC", AllClient = true, IdQueryClient = "CUS-10" };
            ResolveClientAccessRequest? captured = null;
            _clientAccessResolverMock
                .Setup(r => r.ResolveAsync(It.IsAny<ResolveClientAccessRequest>()))
                .Callback<ResolveClientAccessRequest>(r => captured = r)
                .ReturnsAsync(_customersOfCollaborator);
            _reportsDomainServiceMock
                .Setup(s => s.Summarize(It.IsAny<DynamicDataSet>(), It.IsAny<String>(), It.IsAny<Int16>(), It.IsAny<List<CustomersOfCollaboratorDtoResult>?>()))
                .Returns(new List<ReportsSummaryDomainDtoResult>());

            await _sut.ExecuteGetReportsTotalsAsync(request, CancellationToken.None);

            captured.Should().NotBeNull();
            captured!.IdClient.Should().Be("123");
            captured.RoleName.Should().Be("ANALISTASAC");
            captured.AllClient.Should().BeTrue();
            captured.IdQueryClient.Should().Be("CUS-10");
        }

        [Fact]
        public async Task ExecuteGetReportsTotalsAsync_ConsultaLaApiOPENCOMEX()
        {
            var request = new ClientSummaryRequest { IdClient = "123", RoleName = "CLIENT" };
            _reportsDomainServiceMock
                .Setup(s => s.Summarize(It.IsAny<DynamicDataSet>(), "123", It.IsAny<Int16>(), _customersOfCollaborator))
                .Returns(new List<ReportsSummaryDomainDtoResult>());

            await _sut.ExecuteGetReportsTotalsAsync(request, CancellationToken.None);

            _externalDataGatewayMock.Verify(g => g.FetchDataAsync("OPENCOMEX", It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteGetReportsTotalsAsync_ConRolAdmin_ConsultaElDominioSinFiltrarPorCliente()
        {
            var request = new ClientSummaryRequest { IdClient = "admin-1", RoleName = "ADMIN" };
            _reportsDomainServiceMock
                .Setup(s => s.Summarize(It.IsAny<DynamicDataSet>(), String.Empty, 5, _customersOfCollaborator))
                .Returns(new List<ReportsSummaryDomainDtoResult>
                {
                    new() { ClientNit = "111", FrequentRoutes = new List<ReportsFrequentRoutesDomainDtoResult>() },
                    new() { ClientNit = "222", FrequentRoutes = new List<ReportsFrequentRoutesDomainDtoResult>() }
                });

            List<ReportsSummaryResponse> result = await _sut.ExecuteGetReportsTotalsAsync(request, CancellationToken.None);

            result.Select(x => x.ClientNit).Should().ContainInOrder("111", "222");
            request.IdClient.Should().Be(String.Empty);
        }

        [Fact]
        public async Task ExecuteGetReportsTotalsAsync_MapeaCorrectamenteElResultado()
        {
            var request = new ClientSummaryRequest { IdClient = "123", RoleName = "CLIENT" };

            var domainResult = new ReportsSummaryDomainDtoResult
            {
                ClientNit = "123",
                ClientName = "Cliente",
                TotalClientRecords = 20,
                TotalWithIssuesStatus = 1,
                TotalDeliveredStatus = 2,
                TotalDestinationCustomsStatus = 3,
                TotalOriginCustomsStatus = 4,
                TotalInTransitStatus = 5,
                TotalPendingStatus = 6,
                TotalInvoiced = 999.5,
                TotalAdvancePayment = 100.25,
                TotalDelays = 10.5,
                TotalImports = 12,
                TotalExports = 8,
                TotalAirShipments = 7,
                TotalOceanShipments = 13,
                FrequentRoutes = new List<ReportsFrequentRoutesDomainDtoResult>
                {
                    new() { Origin = "BOG", Destination = "MIA", TotalRoute = 3 }
                }
            };
            _reportsDomainServiceMock
                .Setup(s => s.Summarize(It.IsAny<DynamicDataSet>(), "123", 5, _customersOfCollaborator))
                .Returns(new List<ReportsSummaryDomainDtoResult> { domainResult });

            List<ReportsSummaryResponse> results = await _sut.ExecuteGetReportsTotalsAsync(request, CancellationToken.None);

            results.Should().ContainSingle();
            ReportsSummaryResponse result = results[0];
            result.ClientNit.Should().Be("123");
            result.ClientName.Should().Be("Cliente");
            result.TotalClientRecords.Should().Be(20);
            result.TotalWithIssuesStatus.Should().Be(1);
            result.TotalDeliveredStatus.Should().Be(2);
            result.TotalDestinationCustomsStatus.Should().Be(3);
            result.TotalOriginCustomsStatus.Should().Be(4);
            result.TotalInTransitStatus.Should().Be(5);
            result.TotalPendingStatus.Should().Be(6);
            result.TotalInvoiced.Should().Be(999.5);
            result.TotalAdvancePayment.Should().Be(100.25);
            result.TotalDelays.Should().Be(10.5);
            result.TotalImports.Should().Be(12);
            result.TotalExports.Should().Be(8);
            result.TotalAirShipments.Should().Be(7);
            result.TotalOceanShipments.Should().Be(13);
            result.FrequentRoutes.Should().ContainSingle(x => x.Origin == "BOG" && x.Destination == "MIA" && x.TotalRoute == 3);
        }

        [Fact]
        public async Task ExecuteGetReportsTotalsAsync_SinResultadosEnElDominio_RetornaListaVacia()
        {
            var request = new ClientSummaryRequest { IdClient = "123", RoleName = "CLIENT" };
            _reportsDomainServiceMock
                .Setup(s => s.Summarize(It.IsAny<DynamicDataSet>(), "123", 5, _customersOfCollaborator))
                .Returns(new List<ReportsSummaryDomainDtoResult>());

            List<ReportsSummaryResponse> results = await _sut.ExecuteGetReportsTotalsAsync(request, CancellationToken.None);

            results.Should().BeEmpty();
        }
    }
}
