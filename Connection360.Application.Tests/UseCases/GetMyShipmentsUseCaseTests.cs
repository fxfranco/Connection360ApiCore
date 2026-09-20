using Connection360.Application.DTOs;
using Connection360.Application.Ports;
using Connection360.Application.UseCases;
using Connection360.Domain.Dtos;
using Connection360.Domain.Entities;
using Connection360.Domain.Enum;
using Connection360.Domain.Interfaces;
using Connection360.Domain.Services;
using FluentAssertions;
using Moq;
using Xunit;
using static Connection360.Application.Tests.TestSupport.DynamicDataSetBuilder;

namespace Connection360.Application.Tests.UseCases
{
    public class GetMyShipmentsUseCaseTests
    {
        private readonly Mock<IExternalDataGateway> _externalDataGatewayMock = new();
        private readonly Mock<IMyShipmentsDomainService> _myShipmentsDomainServiceMock = new();
        private readonly Mock<IDynamicDataSetMerger> _mergerMock = new();
        private readonly Mock<IExternalApiOpenStreetMap> _openStreetMapMock = new();
        private readonly Mock<IDetailsHistoryShipmentsDomainService> _detailsHistoryServiceMock = new();
        private readonly Mock<IClientAccessResolver> _clientAccessResolverMock = new();
        private readonly List<CustomersOfCollaboratorDtoResult> _customersOfCollaborator = new()
        {
            new() { IdCollaborator = 1, IdentificacionCollaborator = "COL-1", IdCustomer = 10, IdentificacionCustomer = "CUS-10" }
        };
        private readonly GetMyShipmentsUseCase _sut;

        public GetMyShipmentsUseCaseTests()
        {
            _sut = new GetMyShipmentsUseCase(
                _externalDataGatewayMock.Object,
                _myShipmentsDomainServiceMock.Object,
                _mergerMock.Object,
                _openStreetMapMock.Object,
                _detailsHistoryServiceMock.Object,
                _clientAccessResolverMock.Object);

            _clientAccessResolverMock
                .Setup(r => r.ResolveAsync(It.IsAny<ResolveClientAccessRequest>()))
                .ReturnsAsync(_customersOfCollaborator);

            _externalDataGatewayMock
                .Setup(g => g.FetchDataAsync(It.IsAny<String>(), It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Empty());
        }

        private static MyShipmentsDomainResult SampleDomainResult() => new()
        {
            MyShipments = new List<ResumenMyShipmentDto> { new() { Id = 1, DocumentNumber = "HBL-001", ClientName = "Cliente" } },
            ClientSummaryResponse = new ClientSummaryDomainResult { TotalClientRecords = 1, TotalImports = 1 }
        };

        private void SetupResolverRejectsAccess()
        {
            _clientAccessResolverMock
                .Setup(r => r.ResolveAsync(It.IsAny<ResolveClientAccessRequest>()))
                .ThrowsAsync(new ArgumentException("El campo 'cliente' es obligatorio."));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task ExecuteGetAllShipmentsAsync_SiElResolverRechazaElAcceso_PropagaLaExcepcionYNoConsultaElGateway(String? idClient)
        {
            SetupResolverRejectsAccess();
            var request = new MyShipmentsRequest { IdClient = idClient! };

            Func<Task> act = () => _sut.ExecuteGetAllShipmentsAsync(request, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
            _externalDataGatewayMock.Verify(g => g.FetchDataAsync(It.IsAny<String>(), It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteGetAllShipmentsAsync_ResuelveElAccesoConLosDatosDeLaSolicitud()
        {
            var request = new MyShipmentsRequest { IdClient = "123", RoleName = "ANALISTAOPE", AllClient = false, IdQueryClient = "CUS-10", Page = 1, Size = 10 };
            ResolveClientAccessRequest? captured = null;
            _clientAccessResolverMock
                .Setup(r => r.ResolveAsync(It.IsAny<ResolveClientAccessRequest>()))
                .Callback<ResolveClientAccessRequest>(r => captured = r)
                .ReturnsAsync(_customersOfCollaborator);
            _myShipmentsDomainServiceMock
                .Setup(s => s.GetAllShipments(It.IsAny<DynamicDataSet>(), "123", _customersOfCollaborator, 1, 10))
                .Returns(SampleDomainResult());

            await _sut.ExecuteGetAllShipmentsAsync(request, CancellationToken.None);

            captured.Should().NotBeNull();
            captured!.IdClient.Should().Be("123");
            captured.RoleName.Should().Be("ANALISTAOPE");
            captured.AllClient.Should().BeFalse();
            captured.IdQueryClient.Should().Be("CUS-10");
        }

        [Fact]
        public async Task ExecuteGetAllShipmentsAsync_ConRolAdmin_ConsultaElDominioSinFiltrarPorCliente()
        {
            var request = new MyShipmentsRequest { IdClient = "admin-1", RoleName = "ADMIN", Page = 1, Size = 10 };
            _myShipmentsDomainServiceMock
                .Setup(s => s.GetAllShipments(It.IsAny<DynamicDataSet>(), String.Empty, _customersOfCollaborator, 1, 10))
                .Returns(SampleDomainResult());

            MyShipmentsResponse result = await _sut.ExecuteGetAllShipmentsAsync(request, CancellationToken.None);

            result.ClientSummaryResponseData.MyShipments.Should().ContainSingle();
            request.IdClient.Should().Be(String.Empty);
        }

        [Fact]
        public async Task ExecuteGetAllShipmentsAsync_ConsultaLaApiSIM()
        {
            var request = new MyShipmentsRequest { IdClient = "123", Page = 1, Size = 10 };
            _myShipmentsDomainServiceMock.Setup(s => s.GetAllShipments(It.IsAny<DynamicDataSet>(), "123", _customersOfCollaborator, 1, 10)).Returns(SampleDomainResult());

            await _sut.ExecuteGetAllShipmentsAsync(request, CancellationToken.None);

            _externalDataGatewayMock.Verify(g => g.FetchDataAsync("SIM", It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteGetAllShipmentsAsync_MapeaCorrectamenteLaRespuesta()
        {
            var request = new MyShipmentsRequest { IdClient = "123", Page = 1, Size = 10 };
            _myShipmentsDomainServiceMock.Setup(s => s.GetAllShipments(It.IsAny<DynamicDataSet>(), "123", _customersOfCollaborator, 1, 10)).Returns(SampleDomainResult());

            MyShipmentsResponse result = await _sut.ExecuteGetAllShipmentsAsync(request, CancellationToken.None);

            result.ClientSummaryResponseData.TotalClientRecords.Should().Be(1);
            result.ClientSummaryResponseData.MyShipments.Should().ContainSingle(x => x.DocumentNumber == "HBL-001");
            result.ClientSummaryResponseData.TotalWithIssues.Should().BeNull();
        }

        [Fact]
        public async Task ExecuteFilterShipmentsAsync_SiElResolverRechazaElAcceso_PropagaLaExcepcion()
        {
            SetupResolverRejectsAccess();
            var request = new MyShipmentsRequest { IdClient = "", Filters = new MyShipmentsFiltersRequest() };

            Func<Task> act = () => _sut.ExecuteFilterShipmentsAsync(request, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task ExecuteFilterShipmentsAsync_PropagaLosFiltrosAlServicioDeDominio()
        {
            var request = new MyShipmentsRequest
            {
                IdClient = "123",
                Page = 1,
                Size = 10,
                Filters = new MyShipmentsFiltersRequest { ValueFilter = "HBL", OperationType = "IMPO", ShipmentMode = "AIR", State = "Pendiente" }
            };

            MyShipmentsFiltersDto? capturedFilters = null;
            _myShipmentsDomainServiceMock
                .Setup(s => s.GetFiltersShipments(It.IsAny<DynamicDataSet>(), "123", _customersOfCollaborator, 1, 10, It.IsAny<MyShipmentsFiltersDto>()))
                .Callback<DynamicDataSet, String, List<CustomersOfCollaboratorDtoResult>?, Int64, Int64, MyShipmentsFiltersDto>((_, _, _, _, _, f) => capturedFilters = f)
                .Returns(SampleDomainResult());

            await _sut.ExecuteFilterShipmentsAsync(request, CancellationToken.None);

            capturedFilters.Should().NotBeNull();
            capturedFilters!.ValueFilter.Should().Be("HBL");
            capturedFilters.OperationType.Should().Be("IMPO");
            capturedFilters.ShipmentMode.Should().Be("AIR");
            capturedFilters.State.Should().Be("Pendiente");
        }

        [Fact]
        public async Task ExecuteGetHistoryAllShipmentsAsync_MapeaCorrectamenteLaRespuesta()
        {
            var request = new MyShipmentsRequest { IdClient = "123", Page = 1, Size = 10 };
            _myShipmentsDomainServiceMock.Setup(s => s.GetHistoryAllShipments(It.IsAny<DynamicDataSet>(), "123", _customersOfCollaborator, 1, 10)).Returns(SampleDomainResult());

            MyShipmentsResponse result = await _sut.ExecuteGetHistoryAllShipmentsAsync(request, CancellationToken.None);

            result.ClientSummaryResponseData.MyShipments.Should().ContainSingle();
        }

        [Fact]
        public async Task ExecuteFilterHistoryShipmentsAsync_MapeaCorrectamenteLaRespuesta()
        {
            var request = new MyShipmentsRequest { IdClient = "123", Page = 1, Size = 10, Filters = new MyShipmentsFiltersRequest() };
            _myShipmentsDomainServiceMock
                .Setup(s => s.GetFiltersHistoryShipments(It.IsAny<DynamicDataSet>(), "123", _customersOfCollaborator, 1, 10, It.IsAny<MyShipmentsFiltersDto>()))
                .Returns(SampleDomainResult());

            MyShipmentsResponse result = await _sut.ExecuteFilterHistoryShipmentsAsync(request, CancellationToken.None);

            result.ClientSummaryResponseData.MyShipments.Should().ContainSingle();
        }

        [Fact]
        public async Task ExecuteDetailsShipmentsAsync_SiElResolverRechazaElAcceso_PropagaLaExcepcion()
        {
            SetupResolverRejectsAccess();
            var request = new MyShipmentsRequest { IdClient = "", DocumentNumber = "HBL-001" };

            Func<Task> act = () => _sut.ExecuteDetailsShipmentsAsync(request, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task ExecuteDetailsShipmentsAsync_ConDocumentNumberVacio_LanzaArgumentException()
        {
            var request = new MyShipmentsRequest { IdClient = "123", DocumentNumber = "" };

            Func<Task> act = () => _sut.ExecuteDetailsShipmentsAsync(request, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task ExecuteDetailsShipmentsAsync_ConsultaLasSeisApisYFusionaCincoDeEllas()
        {
            var request = new MyShipmentsRequest { IdClient = "123", DocumentNumber = "HBL-001" };
            SetupDetailsHappyPath();

            await _sut.ExecuteDetailsShipmentsAsync(request, CancellationToken.None);

            _externalDataGatewayMock.Verify(g => g.FetchDataAsync("BPMS", It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>()), Times.Once);
            _externalDataGatewayMock.Verify(g => g.FetchDataAsync("SIM", It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>()), Times.Once);
            _externalDataGatewayMock.Verify(g => g.FetchDataAsync("OPENCOMEX", It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>()), Times.Once);
            _externalDataGatewayMock.Verify(g => g.FetchDataAsync("ASISCOMEX", It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>()), Times.Once);
            _externalDataGatewayMock.Verify(g => g.FetchDataAsync("SYSTEMCARRIER", It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>()), Times.Once);
            _externalDataGatewayMock.Verify(g => g.FetchDataAsync("DATALOGS", It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>()), Times.Once);
            _mergerMock.Verify(m => m.Merge(It.Is<IEnumerable<DynamicDataSet>>(sets => sets.Count() == 5), "DOCUMENTO DE TRANSPORTE (HBL)", DataSetJoinType.FullOuter), Times.Once);
        }

        [Fact]
        public async Task ExecuteDetailsShipmentsAsync_ConsultaCoordenadasParaOrigenYDestino()
        {
            var request = new MyShipmentsRequest { IdClient = "123", DocumentNumber = "HBL-001" };
            SetupDetailsHappyPath(origin: "Bogota", destination: "Miami");

            await _sut.ExecuteDetailsShipmentsAsync(request, CancellationToken.None);

            _openStreetMapMock.Verify(o => o.GetCoordinates("OPENSTREETMAP", "Bogota", It.IsAny<CancellationToken>()), Times.Once);
            _openStreetMapMock.Verify(o => o.GetCoordinates("OPENSTREETMAP", "Miami", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteDetailsShipmentsAsync_MapeaLasCoordenadasEnTrackingShipments()
        {
            var request = new MyShipmentsRequest { IdClient = "123", DocumentNumber = "HBL-001" };
            SetupDetailsHappyPath();

            DetailsShipmentsResponse result = await _sut.ExecuteDetailsShipmentsAsync(request, CancellationToken.None);

            result.TrackingShipments.OriginNameCoordinates.Should().Be("Origen Completo");
            result.TrackingShipments.OriginLatitudCoordinates.Should().Be("4.60");
            result.TrackingShipments.DestinationNameCoordinates.Should().Be("Destino Completo");
        }

        [Fact]
        public async Task ExecuteDetailsShipmentsAsync_MapeaElHistorialDeCambios()
        {
            var request = new MyShipmentsRequest { IdClient = "123", DocumentNumber = "HBL-001" };
            SetupDetailsHappyPath();

            DetailsShipmentsResponse result = await _sut.ExecuteDetailsShipmentsAsync(request, CancellationToken.None);

            result.HistoryShipments.DetailsHistoryShipments.Should().ContainSingle(x => x.Message == "Cambio registrado");
        }

        [Fact]
        public async Task ExecuteFilterHistoryShipmentsAsync_SiElResolverRechazaElAcceso_PropagaLaExcepcion()
        {
            SetupResolverRejectsAccess();
            var request = new MyShipmentsRequest { IdClient = "", Filters = new MyShipmentsFiltersRequest() };

            Func<Task> act = () => _sut.ExecuteFilterHistoryShipmentsAsync(request, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task ExecuteGetHistoryAllShipmentsAsync_SiElResolverRechazaElAcceso_PropagaLaExcepcion()
        {
            SetupResolverRejectsAccess();
            var request = new MyShipmentsRequest { IdClient = "" };

            Func<Task> act = () => _sut.ExecuteGetHistoryAllShipmentsAsync(request, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task ExecuteFilterHistoryShipmentsAsync_PropagaLosFiltrosAlServicioDeDominio()
        {
            var request = new MyShipmentsRequest
            {
                IdClient = "123",
                Page = 1,
                Size = 10,
                Filters = new MyShipmentsFiltersRequest { ValueFilter = "HBL", OperationType = "EXPO", ShipmentMode = "SEA", State = "Entregado" }
            };

            MyShipmentsFiltersDto? capturedFilters = null;
            _myShipmentsDomainServiceMock
                .Setup(s => s.GetFiltersHistoryShipments(It.IsAny<DynamicDataSet>(), "123", _customersOfCollaborator, 1, 10, It.IsAny<MyShipmentsFiltersDto>()))
                .Callback<DynamicDataSet, String, List<CustomersOfCollaboratorDtoResult>?, Int64, Int64, MyShipmentsFiltersDto>((_, _, _, _, _, f) => capturedFilters = f)
                .Returns(SampleDomainResult());

            await _sut.ExecuteFilterHistoryShipmentsAsync(request, CancellationToken.None);

            capturedFilters.Should().NotBeNull();
            capturedFilters!.ValueFilter.Should().Be("HBL");
            capturedFilters.OperationType.Should().Be("EXPO");
            capturedFilters.ShipmentMode.Should().Be("SEA");
            capturedFilters.State.Should().Be("Entregado");
        }

        [Fact]
        public async Task ExecuteGetAllShipmentsAsync_MapeaTodosLosCamposDelEnvioYLosTotales()
        {
            var request = new MyShipmentsRequest { IdClient = "123", Page = 1, Size = 10 };
            var fecha = new DateTime(2024, 6, 1);
            _myShipmentsDomainServiceMock
                .Setup(s => s.GetAllShipments(It.IsAny<DynamicDataSet>(), "123", _customersOfCollaborator, 1, 10))
                .Returns(new MyShipmentsDomainResult
                {
                    MyShipments = new List<ResumenMyShipmentDto>
                    {
                        new()
                        {
                            Id = 7,
                            ClientNit = "123",
                            ShipmentMode = "AIR",
                            DocumentNumber = "HBL-007",
                            State = "Pendiente",
                            OperationType = "IMPO",
                            ClientName = "Cliente",
                            Origin = "BOG",
                            Destination = "MIA",
                            ETDDate = fecha,
                            ATDDate = fecha.AddDays(1),
                            ETADate = fecha.AddDays(2),
                            ATADate = fecha.AddDays(3)
                        }
                    },
                    ClientSummaryResponse = new ClientSummaryDomainResult
                    {
                        TotalClientRecords = 4,
                        TotalImports = 3,
                        TotalExports = 1,
                        TotalAirShipments = 2,
                        TotalOceanShipments = 2
                    }
                });

            MyShipmentsResponse result = await _sut.ExecuteGetAllShipmentsAsync(request, CancellationToken.None);

            result.ClientSummaryResponseData.TotalClientRecords.Should().Be(4);
            result.ClientSummaryResponseData.TotalImports.Should().Be(3);
            result.ClientSummaryResponseData.TotalExports.Should().Be(1);
            result.ClientSummaryResponseData.TotalAirShipments.Should().Be(2);
            result.ClientSummaryResponseData.TotalOceanShipments.Should().Be(2);

            ResumeMyShipmentsResponse shipment = result.ClientSummaryResponseData.MyShipments!.Single();
            shipment.Id.Should().Be(7);
            shipment.ClientNit.Should().Be("123");
            shipment.ShipmentMode.Should().Be("AIR");
            shipment.DocumentNumber.Should().Be("HBL-007");
            shipment.State.Should().Be("Pendiente");
            shipment.OperationType.Should().Be("IMPO");
            shipment.ClientName.Should().Be("Cliente");
            shipment.Origin.Should().Be("BOG");
            shipment.Destination.Should().Be("MIA");
            shipment.ETDDate.Should().Be(fecha);
            shipment.ATDDate.Should().Be(fecha.AddDays(1));
            shipment.ETADate.Should().Be(fecha.AddDays(2));
            shipment.ATADate.Should().Be(fecha.AddDays(3));
        }

        private void SetupDetailsHappyPath(String origin = "Bogota", String destination = "Miami")
        {
            _mergerMock
                .Setup(m => m.Merge(It.IsAny<IEnumerable<DynamicDataSet>>(), It.IsAny<String>(), It.IsAny<DataSetJoinType>()))
                .Returns(Empty());

            _myShipmentsDomainServiceMock
                .Setup(s => s.GetDetailsShipments(It.IsAny<DynamicDataSet>(), "123", _customersOfCollaborator, "HBL-001"))
                .Returns(new DetailsShipmentsDomainDtoResult
                {
                    ResumenShipments = new SummaryShipmentsDomainDtoResult { Origin = origin, Destination = destination },
                    TrackingShipments = new TrackingShipmentsDomainDtoResult { State = "En tránsito" },
                    LogisticsDatesShipments = new LogisticsDatesShipmentsDomainDtoResult(),
                    ContainerShipments = new ContainerShipmentsDomainDtoResult(),
                    FinancialInfoShipments = new FinancialInfoShipmentsDomainDtoResult()
                });

            _detailsHistoryServiceMock
                .Setup(s => s.GetDetailsHistoryShipments(It.IsAny<DynamicDataSet>(), "HBL-001"))
                .Returns(new HistoryShipmentsDomainDtoResult
                {
                    DetailsHistoryShipments = new List<DetailsHistoryShipmentsDomainDtoResult> { new() { Message = "Cambio registrado" } }
                });

            _openStreetMapMock
                .Setup(o => o.GetCoordinates("OPENSTREETMAP", origin, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OpenStreetMapDto { PlaceName = "Origen Completo", Latitud = "4.60", Longitud = "-74.08" });

            _openStreetMapMock
                .Setup(o => o.GetCoordinates("OPENSTREETMAP", destination, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OpenStreetMapDto { PlaceName = "Destino Completo", Latitud = "25.76", Longitud = "-80.19" });
        }
    }
}
