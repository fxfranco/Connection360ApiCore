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
    /// <summary>
    /// Complementa GetMyShipmentsUseCaseTests: rama de rol ADMIN en el resto de operaciones y mapeo completo del detalle.
    /// </summary>
    public class GetMyShipmentsUseCaseAdminTests
    {
        private readonly Mock<IApplicationDataSheetDataGateway> _appGateway = new();
        private readonly Mock<ILogStatusTrackingDataGateway> _logGateway = new();
        private readonly Mock<IMyShipmentsDomainService> _domain = new();
        private readonly Mock<IDynamicDataSetMerger> _merger = new();
        private readonly Mock<IExternalApiOpenStreetMap> _osm = new();
        private readonly Mock<IDetailsHistoryShipmentsDomainService> _history = new();
        private readonly Mock<IClientAccessResolver> _resolver = new();
        private readonly List<CustomersOfCollaboratorDtoResult> _customers = new();
        private readonly GetMyShipmentsUseCase _sut;

        public GetMyShipmentsUseCaseAdminTests()
        {
            _sut = new GetMyShipmentsUseCase(_appGateway.Object, _logGateway.Object, _domain.Object, _merger.Object, _osm.Object, _history.Object, _resolver.Object);
            _resolver.Setup(r => r.ResolveAsync(It.IsAny<ResolveClientAccessRequest>())).ReturnsAsync(_customers);
            _appGateway.Setup(g => g.FetchDataAsync(It.IsAny<ApplicationDataSheetDataRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(Empty());
            _logGateway.Setup(g => g.FetchDataAsync(It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>())).ReturnsAsync(Empty());
        }

        private static MyShipmentsDomainResult DomainResult() => new()
        {
            MyShipments = new List<ResumenMyShipmentDto>(),
            ClientSummaryResponse = new ClientSummaryDomainResult()
        };

        [Fact]
        public async Task ExecuteFilterShipmentsAsync_ConRolAdmin_ConsultaElDominioSinFiltrarPorCliente()
        {
            var request = new MyShipmentsRequest { IdClient = "adm", RoleName = "ADMIN", Page = 2, Size = 5, Filters = new MyShipmentsFiltersRequest() };
            _domain.Setup(s => s.GetFiltersShipments(It.IsAny<DynamicDataSet>(), String.Empty, _customers, 2, 5, It.IsAny<MyShipmentsFiltersDto>())).Returns(DomainResult());

            MyShipmentsResponse result = await _sut.ExecuteFilterShipmentsAsync(request, CancellationToken.None);

            result.ClientSummaryResponseData.MyShipments.Should().BeEmpty();
            request.IdClient.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteGetHistoryAllShipmentsAsync_ConRolAdmin_ConsultaElDominioSinFiltrarPorCliente()
        {
            var request = new MyShipmentsRequest { IdClient = "adm", RoleName = "ADMIN", Page = 1, Size = 5 };
            _domain.Setup(s => s.GetHistoryAllShipments(It.IsAny<DynamicDataSet>(), String.Empty, _customers, 1, 5)).Returns(DomainResult());

            MyShipmentsResponse result = await _sut.ExecuteGetHistoryAllShipmentsAsync(request, CancellationToken.None);

            result.ClientSummaryResponseData.MyShipments.Should().BeEmpty();
            request.IdClient.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteFilterHistoryShipmentsAsync_ConRolAdmin_ConsultaElDominioSinFiltrarPorCliente()
        {
            var request = new MyShipmentsRequest { IdClient = "adm", RoleName = "ADMIN", Page = 1, Size = 5, Filters = new MyShipmentsFiltersRequest() };
            _domain.Setup(s => s.GetFiltersHistoryShipments(It.IsAny<DynamicDataSet>(), String.Empty, _customers, 1, 5, It.IsAny<MyShipmentsFiltersDto>())).Returns(DomainResult());

            MyShipmentsResponse result = await _sut.ExecuteFilterHistoryShipmentsAsync(request, CancellationToken.None);

            result.ClientSummaryResponseData.MyShipments.Should().BeEmpty();
            request.IdClient.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteDetailsShipmentsAsync_ConRolAdmin_ConsultaElDominioSinFiltrarPorCliente()
        {
            var request = new MyShipmentsRequest { IdClient = "adm", RoleName = "ADMIN", DocumentNumber = "HBL-9" };
            SetupDetails("HBL-9", String.Empty);

            DetailsShipmentsResponse result = await _sut.ExecuteDetailsShipmentsAsync(request, CancellationToken.None);

            result.Should().NotBeNull();
            request.IdClient.Should().BeEmpty();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ExecuteDetailsShipmentsAsync_SinDocumento_NoResuelveAccesoNiConsultaGateways(String? document)
        {
            var request = new MyShipmentsRequest { IdClient = "1", DocumentNumber = document! };

            Func<Task> act = () => _sut.ExecuteDetailsShipmentsAsync(request, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Documento*");
            _resolver.Verify(r => r.ResolveAsync(It.IsAny<ResolveClientAccessRequest>()), Times.Never);
            _appGateway.VerifyNoOtherCalls();
            _logGateway.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ExecuteDetailsShipmentsAsync_MapeaTodasLasSecciones()
        {
            var request = new MyShipmentsRequest { IdClient = "1", DocumentNumber = "HBL-9" };
            SetupDetails("HBL-9", "1");

            DetailsShipmentsResponse r = await _sut.ExecuteDetailsShipmentsAsync(request, CancellationToken.None);

            r.ResumenShipments.Id.Should().Be("7");
            r.ResumenShipments.ClientId.Should().Be("NIT-1");
            r.ResumenShipments.ClientName.Should().Be("Cliente");
            r.ResumenShipments.Supplier.Should().Be("Prov");
            r.ResumenShipments.Carrier.Should().Be("Carrier");
            r.ResumenShipments.MerchandiseDescription.Should().Be("Mercancia");
            r.ResumenShipments.DocumentNumber.Should().Be("HBL-9");
            r.ResumenShipments.DocumentType.Should().Be("HBL");
            r.ResumenShipments.Origin.Should().Be("Bogota");
            r.ResumenShipments.Destination.Should().Be("Miami");
            r.ResumenShipments.LoadType.Should().Be("FCL");
            r.ResumenShipments.PackagesNumbers.Should().Be("10");
            r.ResumenShipments.WeightKg.Should().Be("100");
            r.ResumenShipments.VolumeM3.Should().Be("5");
            r.ResumenShipments.Incoterm.Should().Be("FOB");
            r.ResumenShipments.OperationType.Should().Be("IMPO");
            r.ResumenShipments.ShipmentMode.Should().Be("SEA");

            r.TrackingShipments.State.Should().Be("En transito");
            r.TrackingShipments.OriginLongitudCoordinates.Should().Be("-74");
            r.TrackingShipments.DestinationLatitudCoordinates.Should().Be("25");
            r.TrackingShipments.DestinationLongitudCoordinates.Should().Be("-80");

            r.LogisticsDatesShipments.StoreOriginDate.Should().Be(new DateTime(2025, 1, 1));
            r.LogisticsDatesShipments.ETDDate.Should().Be(new DateTime(2025, 1, 2));
            r.LogisticsDatesShipments.ATDDate.Should().Be(new DateTime(2025, 1, 3));
            r.LogisticsDatesShipments.ETADate.Should().Be(new DateTime(2025, 1, 4));
            r.LogisticsDatesShipments.ATADate.Should().Be(new DateTime(2025, 1, 5));
            r.LogisticsDatesShipments.StoreDestinationDate.Should().Be(new DateTime(2025, 1, 6));
            r.LogisticsDatesShipments.NationalizationDate.Should().Be(new DateTime(2025, 1, 7));
            r.LogisticsDatesShipments.DispatchDestinationDate.Should().Be(new DateTime(2025, 1, 8));
            r.LogisticsDatesShipments.FormDate.Should().Be(new DateTime(2025, 1, 9));
            r.LogisticsDatesShipments.ContainerDeliveryDate.Should().Be(new DateTime(2025, 1, 10));

            r.ContainerShipments.ContainerType.Should().Be("40HC");
            r.ContainerShipments.ContainerAmount.Should().Be("2");
            r.ContainerShipments.ContainerNumber.Should().Be("MSKU1");
            r.ContainerShipments.DaysOff.Should().Be("7");
            r.ContainerShipments.DaysRemainingDelivery.Should().Be("3");
            r.ContainerShipments.ActualContainerReturnDate.Should().Be(new DateTime(2025, 2, 1));
            r.ContainerShipments.ContainerDelayDays.Should().Be("1");
            r.ContainerShipments.CostDayOfDelay.Should().Be("11");
            r.ContainerShipments.TotalCostContainerDelays.Should().Be("12");
            r.ContainerShipments.ContainerDepot.Should().Be("13");

            r.FinancialInfoShipments.AdvancePaymentRequestDate.Should().Be(new DateTime(2025, 3, 1));
            r.FinancialInfoShipments.AdvancePaymentDate.Should().Be(new DateTime(2025, 3, 2));
            r.FinancialInfoShipments.AdvancePaymentAmount.Should().Be("a");
            r.FinancialInfoShipments.SupplierInvoice.Should().Be("b");
            r.FinancialInfoShipments.TCCInvoice.Should().Be("c");
            r.FinancialInfoShipments.InvoiceNumber.Should().Be("d");
            r.FinancialInfoShipments.InvoiceDate.Should().Be("e");
            r.FinancialInfoShipments.ExpenseDescription.Should().Be("f");
            r.FinancialInfoShipments.ExpenseAmountUSD.Should().Be("g");
            r.FinancialInfoShipments.InvoiceSubtotalUSD.Should().Be("h");
            r.FinancialInfoShipments.IvaUSD.Should().Be("i");
            r.FinancialInfoShipments.TotalInvoiceUSD.Should().Be("j");

            DetailsHistoryShipmentsResponse h = r.HistoryShipments.DetailsHistoryShipments!.Should().ContainSingle().Subject;
            h.ChangeDate.Should().Be(new DateTime(2025, 4, 1));
            h.ChangeUser.Should().Be("user");
            h.Message.Should().Be("msg");
            h.OldState.Should().Be("old");
            h.NewState.Should().Be("new");
        }

        [Fact]
        public async Task ExecuteDetailsShipmentsAsync_SiLaConsultaDeCoordenadasFalla_PropagaLaExcepcion()
        {
            var request = new MyShipmentsRequest { IdClient = "1", DocumentNumber = "HBL-9" };
            SetupDetails("HBL-9", "1");
            _osm.Setup(o => o.GetCoordinates("OPENSTREETMAP", "Miami", It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException("osm"));

            Func<Task> act = () => _sut.ExecuteDetailsShipmentsAsync(request, CancellationToken.None);

            await act.Should().ThrowAsync<HttpRequestException>();
        }

        private void SetupDetails(String document, String expectedClientId)
        {
            _domain.Setup(s => s.GetDetailsShipments(It.IsAny<DynamicDataSet>(), expectedClientId, _customers, document))
                .Returns(new DetailsShipmentsDomainDtoResult
                {
                    ResumenShipments = new SummaryShipmentsDomainDtoResult
                    {
                        Id = "7", ClientNit = "NIT-1", ClientName = "Cliente", Supplier = "Prov", Carrier = "Carrier",
                        MerchandiseDescription = "Mercancia", DocumentNumber = "HBL-9", DocumentType = "HBL", Origin = "Bogota",
                        Destination = "Miami", LoadType = "FCL", PackagesNumbers = "10", WeightKg = "100", VolumeM3 = "5",
                        Incoterm = "FOB", OperationType = "IMPO", ShipmentMode = "SEA"
                    },
                    TrackingShipments = new TrackingShipmentsDomainDtoResult { State = "En transito" },
                    LogisticsDatesShipments = new LogisticsDatesShipmentsDomainDtoResult
                    {
                        StoreOriginDate = new DateTime(2025, 1, 1), ETDDate = new DateTime(2025, 1, 2), ATDDate = new DateTime(2025, 1, 3),
                        ETADate = new DateTime(2025, 1, 4), ATADate = new DateTime(2025, 1, 5), StoreDestinationDate = new DateTime(2025, 1, 6),
                        NationalizationDate = new DateTime(2025, 1, 7), DispatchDestinationDate = new DateTime(2025, 1, 8),
                        FormDate = new DateTime(2025, 1, 9), ContainerDeliveryDate = new DateTime(2025, 1, 10)
                    },
                    ContainerShipments = new ContainerShipmentsDomainDtoResult
                    {
                        ContainerType = "40HC", ContainerAmount = "2", ContainerNumber = "MSKU1", DaysOff = "7", DaysRemainingDelivery = "3",
                        ActualContainerReturnDate = new DateTime(2025, 2, 1), ContainerDelayDays = "1", CostDayOfDelay = "11",
                        TotalCostContainerDelays = "12", ContainerDepot = "13"
                    },
                    FinancialInfoShipments = new FinancialInfoShipmentsDomainDtoResult
                    {
                        AdvancePaymentRequestDate = new DateTime(2025, 3, 1), AdvancePaymentDate = new DateTime(2025, 3, 2),
                        AdvancePaymentAmount = "a", SupplierInvoice = "b", TCCInvoice = "c", InvoiceNumber = "d", InvoiceDate = "e",
                        ExpenseDescription = "f", ExpenseAmountUSD = "g", InvoiceSubtotalUSD = "h", IvaUSD = "i", TotalInvoiceUSD = "j"
                    }
                });

            _history.Setup(s => s.GetDetailsHistoryShipments(It.IsAny<DynamicDataSet>(), document))
                .Returns(new HistoryShipmentsDomainDtoResult
                {
                    DetailsHistoryShipments = new List<DetailsHistoryShipmentsDomainDtoResult>
                    {
                        new() { ChangeDate = new DateTime(2025, 4, 1), ChangeUser = "user", Message = "msg", OldState = "old", NewState = "new" }
                    }
                });

            _osm.Setup(o => o.GetCoordinates("OPENSTREETMAP", "Bogota", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OpenStreetMapDto { PlaceName = "O", Latitud = "4", Longitud = "-74" });
            _osm.Setup(o => o.GetCoordinates("OPENSTREETMAP", "Miami", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OpenStreetMapDto { PlaceName = "D", Latitud = "25", Longitud = "-80" });
        }
    }
}
