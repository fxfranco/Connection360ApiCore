using Connection360.Domain.Constans;
using Connection360.Domain.Dtos;
using Connection360.Domain.Entities;
using Connection360.Domain.Interfaces;
using Connection360.Domain.Services;
using FluentAssertions;
using Moq;
using Xunit;
using static Connection360.Domain.Tests.TestSupport.DynamicDataSetBuilder;

namespace Connection360.Domain.Tests.Services
{
    /// <summary>
    /// Casos complementarios de MyShipmentsDomainService: ordenamiento, paginacion, totales tras filtrar,
    /// mapeo de fechas y secciones del detalle.
    /// </summary>
    public class MyShipmentsDomainServiceComplementTests
    {
        private const String ClientId = "900123456";
        private readonly MyShipmentsDomainService _sut = new(new ClientRecordsFilterService());

        private static DynamicRecord ShipmentRow(String id, String state = ExternalDataValues.PendingState, String operationType = ExternalDataValues.Import,
            String shipmentMode = ExternalDataValues.AirShipment, String origin = "BOG", String destination = "MIA", String clientName = "Cliente Demo",
            String documentNumber = "", String creationDate = "01/01/2024", String clientNit = ClientId)
        {
            return Row(
                (ExternalDataFields.ID, id),
                (ExternalDataFields.ClientNit, clientNit),
                (ExternalDataFields.State, state),
                (ExternalDataFields.OperationType, operationType),
                (ExternalDataFields.ShipmentMode, shipmentMode),
                (ExternalDataFields.ClientName, clientName),
                (ExternalDataFields.Origin, origin),
                (ExternalDataFields.Destination, destination),
                (ExternalDataFields.DocumentNumber, String.IsNullOrEmpty(documentNumber) ? $"HBL-{id}" : documentNumber),
                (ExternalDataFields.CreationDate, creationDate));
        }

        private static DynamicDataSet Set(params DynamicRecord[] rows) => DataSet(new[] { ExternalDataFields.ClientNit }, rows);

        // ---------- Delegacion en IClientRecordsFilterService ----------

        [Fact]
        public void GetAllShipments_DelegaElFiltradoAlServicioDeFiltroConLosMismosArgumentos()
        {
            var filterMock = new Mock<IClientRecordsFilterService>();
            var dataSet = Set(ShipmentRow("1"));
            var customers = new List<CustomersOfCollaboratorDtoResult> { new() { IdentificacionCustomer = "X" } };
            filterMock.Setup(f => f.Filter(dataSet, "C", customers)).Returns(new List<DynamicRecord>());
            var sut = new MyShipmentsDomainService(filterMock.Object);

            MyShipmentsDomainResult result = sut.GetAllShipments(dataSet, "C", customers, 1, 10);

            result.MyShipments.Should().BeEmpty();
            result.ClientSummaryResponse.TotalClientRecords.Should().Be(0);
            filterMock.Verify(f => f.Filter(dataSet, "C", customers), Times.Once);
        }

        [Fact]
        public void GetFiltersHistoryShipments_DelegaElFiltradoAlServicioDeFiltro()
        {
            var filterMock = new Mock<IClientRecordsFilterService>();
            var dataSet = Set();
            filterMock.Setup(f => f.Filter(dataSet, "C", null)).Returns(new List<DynamicRecord> { ShipmentRow("1") });
            var sut = new MyShipmentsDomainService(filterMock.Object);

            MyShipmentsDomainResult result = sut.GetFiltersHistoryShipments(dataSet, "C", null, 1, 10, new MyShipmentsFiltersDto());

            result.MyShipments.Should().ContainSingle();
            filterMock.Verify(f => f.Filter(dataSet, "C", null), Times.Once);
        }

        // ---------- GetAllShipments / GetHistoryAllShipments ----------

        [Fact]
        public void GetAllShipments_SinRegistros_RetornaListaVaciaYTotalesEnCero()
        {
            MyShipmentsDomainResult result = _sut.GetAllShipments(Set(), ClientId, null, 1, 10);

            result.MyShipments.Should().BeEmpty();
            result.ClientSummaryResponse.TotalClientRecords.Should().Be(0);
            result.ClientSummaryResponse.TotalImports.Should().Be(0);
            result.ClientSummaryResponse.TotalExports.Should().Be(0);
            result.ClientSummaryResponse.TotalAirShipments.Should().Be(0);
            result.ClientSummaryResponse.TotalOceanShipments.Should().Be(0);
        }

        [Fact]
        public void GetAllShipments_OrdenaPorIdAscendente()
        {
            var dataSet = Set(ShipmentRow("30"), ShipmentRow("10"), ShipmentRow("20"));

            MyShipmentsDomainResult result = _sut.GetAllShipments(dataSet, ClientId, null, 1, 10);

            result.MyShipments.Select(x => x.Id).Should().Equal(10, 20, 30);
        }

        [Fact]
        public void GetAllShipments_ConIdsIgualesDesempataPorFechaDeCreacionDescendente()
        {
            var dataSet = Set(
                ShipmentRow("5", documentNumber: "VIEJO", creationDate: "01/01/2023"),
                ShipmentRow("5", documentNumber: "NUEVO", creationDate: "01/01/2025"),
                ShipmentRow("5", documentNumber: "MEDIO", creationDate: "01/01/2024"));

            MyShipmentsDomainResult result = _sut.GetAllShipments(dataSet, ClientId, null, 1, 10);

            result.MyShipments.Select(x => x.DocumentNumber).Should().Equal("NUEVO", "MEDIO", "VIEJO");
        }

        [Fact]
        public void GetAllShipments_ConIdNoNumerico_LoTratarComoCeroYLoOrdenaPrimero()
        {
            var dataSet = Set(ShipmentRow("7"), ShipmentRow("abc"), ShipmentRow(""));

            MyShipmentsDomainResult result = _sut.GetAllShipments(dataSet, ClientId, null, 1, 10);

            result.MyShipments.Select(x => x.Id).Should().Equal(0, 0, 7);
        }

        [Theory]
        [InlineData(1, 2, new Int64[] { 1, 2 })]
        [InlineData(2, 2, new Int64[] { 3, 4 })]
        [InlineData(3, 2, new Int64[] { 5 })]
        [InlineData(4, 2, new Int64[0])]
        [InlineData(1, 10, new Int64[] { 1, 2, 3, 4, 5 })]
        [InlineData(1, 0, new Int64[0])]
        public void GetAllShipments_Paginacion_RetornaLaPaginaEsperada(Int64 page, Int64 size, Int64[] expectedIds)
        {
            var dataSet = Set(Enumerable.Range(1, 5).Select(i => ShipmentRow(i.ToString())).ToArray());

            MyShipmentsDomainResult result = _sut.GetAllShipments(dataSet, ClientId, null, page, size);

            result.MyShipments.Select(x => x.Id).Should().Equal(expectedIds);
            result.ClientSummaryResponse.TotalClientRecords.Should().Be(5);
        }

        [Fact]
        public void GetAllShipments_ConPaginaCero_TratarloComoDesdeElInicio()
        {
            var dataSet = Set(Enumerable.Range(1, 3).Select(i => ShipmentRow(i.ToString())).ToArray());

            MyShipmentsDomainResult result = _sut.GetAllShipments(dataSet, ClientId, null, 0, 2);

            result.MyShipments.Select(x => x.Id).Should().Equal(1, 2);
        }

        [Fact]
        public void GetAllShipments_LosTotalesNoDependenDeLaPaginacion()
        {
            var dataSet = Set(
                ShipmentRow("1", operationType: ExternalDataValues.Import, shipmentMode: ExternalDataValues.AirShipment),
                ShipmentRow("2", operationType: ExternalDataValues.Import, shipmentMode: ExternalDataValues.OceanShipment),
                ShipmentRow("3", operationType: ExternalDataValues.Export, shipmentMode: ExternalDataValues.OceanShipment));

            MyShipmentsDomainResult result = _sut.GetAllShipments(dataSet, ClientId, null, 1, 1);

            result.MyShipments.Should().HaveCount(1);
            result.ClientSummaryResponse.TotalClientRecords.Should().Be(3);
            result.ClientSummaryResponse.TotalImports.Should().Be(2);
            result.ClientSummaryResponse.TotalExports.Should().Be(1);
            result.ClientSummaryResponse.TotalAirShipments.Should().Be(1);
            result.ClientSummaryResponse.TotalOceanShipments.Should().Be(2);
        }

        [Fact]
        public void GetAllShipments_MapeaTodosLosCamposYParseaLasFechas()
        {
            var row = Row(
                (ExternalDataFields.ID, "42"),
                (ExternalDataFields.ClientNit, ClientId),
                (ExternalDataFields.ShipmentMode, "SEA"),
                (ExternalDataFields.DocumentNumber, "HBL-42"),
                (ExternalDataFields.State, "Pendiente"),
                (ExternalDataFields.OperationType, "EXPO"),
                (ExternalDataFields.ClientName, "ACME"),
                (ExternalDataFields.Origin, "Cartagena"),
                (ExternalDataFields.Destination, "Rotterdam"),
                (ExternalDataFields.ETDDate, "01/02/2024"),
                (ExternalDataFields.ATDDate, "02/02/2024 10:30:00"),
                (ExternalDataFields.ETADate, "2024-03-01"),
                (ExternalDataFields.ATADate, "no-es-fecha"));

            ResumenMyShipmentDto dto = _sut.GetAllShipments(Set(row), ClientId, null, 1, 10).MyShipments.Single();

            dto.Id.Should().Be(42);
            dto.ClientNit.Should().Be(ClientId);
            dto.ShipmentMode.Should().Be("SEA");
            dto.DocumentNumber.Should().Be("HBL-42");
            dto.State.Should().Be("Pendiente");
            dto.OperationType.Should().Be("EXPO");
            dto.ClientName.Should().Be("ACME");
            dto.Origin.Should().Be("Cartagena");
            dto.Destination.Should().Be("Rotterdam");
            dto.ETDDate.Should().Be(new DateTime(2024, 2, 1));
            dto.ATDDate.Should().Be(new DateTime(2024, 2, 2, 10, 30, 0));
            dto.ETADate.Should().Be(new DateTime(2024, 3, 1));
            dto.ATADate.Should().Be(DateTime.MinValue);
        }

        [Fact]
        public void GetAllShipments_ConCamposFaltantesEnElRegistro_RetornaValoresPorDefecto()
        {
            ResumenMyShipmentDto dto = _sut.GetAllShipments(Set(Row((ExternalDataFields.ClientNit, ClientId))), ClientId, null, 1, 10).MyShipments.Single();

            dto.Id.Should().Be(0);
            dto.DocumentNumber.Should().BeEmpty();
            dto.ETDDate.Should().Be(DateTime.MinValue);
        }

        [Fact]
        public void GetHistoryAllShipments_NoDescartaPorEstado_YCalculaTotalesYPaginacion()
        {
            var dataSet = Set(
                ShipmentRow("1", state: ExternalDataValues.DeliveredState, operationType: ExternalDataValues.Export, shipmentMode: ExternalDataValues.OceanShipment),
                ShipmentRow("2", state: ExternalDataValues.DeliveredState, operationType: ExternalDataValues.Import, shipmentMode: ExternalDataValues.AirShipment),
                ShipmentRow("3", state: ExternalDataValues.PendingState, operationType: ExternalDataValues.Import, shipmentMode: ExternalDataValues.AirShipment));

            MyShipmentsDomainResult page2 = _sut.GetHistoryAllShipments(dataSet, ClientId, null, 2, 2);

            page2.MyShipments.Select(x => x.Id).Should().Equal(3);
            page2.ClientSummaryResponse.TotalClientRecords.Should().Be(3);
            page2.ClientSummaryResponse.TotalImports.Should().Be(2);
            page2.ClientSummaryResponse.TotalExports.Should().Be(1);
            page2.ClientSummaryResponse.TotalAirShipments.Should().Be(2);
            page2.ClientSummaryResponse.TotalOceanShipments.Should().Be(1);
        }

        [Fact]
        public void GetHistoryAllShipments_ConClientesDeColaborador_FiltraPorSusClientes()
        {
            var dataSet = Set(ShipmentRow("1", clientNit: "A"), ShipmentRow("2", clientNit: "B"), ShipmentRow("3", clientNit: "C"));
            var customers = new List<CustomersOfCollaboratorDtoResult>
            {
                new() { IdentificacionCustomer = "a" }, new() { IdentificacionCustomer = "B" }
            };

            MyShipmentsDomainResult result = _sut.GetHistoryAllShipments(dataSet, "COL", customers, 1, 10);

            result.MyShipments.Select(x => x.Id).Should().Equal(1, 2);
        }

        [Fact]
        public void GetAllShipments_SinClientId_RetornaTodosLosRegistros()
        {
            var dataSet = Set(ShipmentRow("1", clientNit: "A"), ShipmentRow("2", clientNit: "B"));

            MyShipmentsDomainResult result = _sut.GetAllShipments(dataSet, String.Empty, null, 1, 10);

            result.MyShipments.Should().HaveCount(2);
        }

        // ---------- GetFiltersShipments ----------

        [Theory]
        [InlineData("hbl-2", 2)]
        [InlineData("ACME", 3)]
        [InlineData("medell", 4)]
        [InlineData("miami", 5)]
        public void GetFiltersShipments_ValueFilter_CoincideEnCadaUnoDeLosCuatroCampos(String value, Int64 expectedId)
        {
            var dataSet = Set(
                ShipmentRow("1", origin: "BOG", destination: "MIA", clientName: "Otro"),
                ShipmentRow("2", origin: "BOG", destination: "MIA", clientName: "Otro"),
                ShipmentRow("3", origin: "BOG", destination: "MIA", clientName: "Acme SAS"),
                ShipmentRow("4", origin: "Medellin", destination: "MIA", clientName: "Otro"),
                ShipmentRow("5", origin: "BOG", destination: "Miami", clientName: "Otro"));

            MyShipmentsDomainResult result = _sut.GetFiltersShipments(dataSet, ClientId, null, 1, 10, new MyShipmentsFiltersDto { ValueFilter = value });

            result.MyShipments.Select(x => x.Id).Should().Contain(expectedId);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void GetFiltersShipments_FiltrosEnBlanco_SeIgnoran(String? blank)
        {
            var dataSet = Set(ShipmentRow("1"), ShipmentRow("2", operationType: ExternalDataValues.Export));
            var filters = new MyShipmentsFiltersDto { ValueFilter = blank, OperationType = blank, ShipmentMode = blank, State = blank };

            MyShipmentsDomainResult result = _sut.GetFiltersShipments(dataSet, ClientId, null, 1, 10, filters);

            result.MyShipments.Should().HaveCount(2);
        }

        [Fact]
        public void GetFiltersShipments_OperationTypeYShipmentMode_SeCompararConIgualdadExactaConMayusculas()
        {
            var dataSet = Set(
                ShipmentRow("1", operationType: "IMPO", shipmentMode: "AIR"),
                ShipmentRow("2", operationType: "impo", shipmentMode: "air"));

            MyShipmentsDomainResult byOperation = _sut.GetFiltersShipments(dataSet, ClientId, null, 1, 10, new MyShipmentsFiltersDto { OperationType = "IMPO" });
            MyShipmentsDomainResult byMode = _sut.GetFiltersShipments(dataSet, ClientId, null, 1, 10, new MyShipmentsFiltersDto { ShipmentMode = "AIR" });

            byOperation.MyShipments.Select(x => x.Id).Should().Equal(1);
            byMode.MyShipments.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void GetFiltersShipments_StateConRegistroSinEstado_NoLoIncluye()
        {
            var dataSet = Set(ShipmentRow("1", state: ""), ShipmentRow("2", state: ExternalDataValues.InTransitState));

            MyShipmentsDomainResult result = _sut.GetFiltersShipments(dataSet, ClientId, null, 1, 10, new MyShipmentsFiltersDto { State = ExternalDataValues.InTransitState });

            result.MyShipments.Select(x => x.Id).Should().Equal(2);
        }

        [Fact]
        public void GetFiltersShipments_CombinaTodosLosFiltros()
        {
            var dataSet = Set(
                ShipmentRow("1", origin: "BOGOTA", operationType: "IMPO", shipmentMode: "AIR", state: ExternalDataValues.InTransitState),
                ShipmentRow("2", origin: "BOGOTA", operationType: "EXPO", shipmentMode: "AIR", state: ExternalDataValues.InTransitState),
                ShipmentRow("3", origin: "BOGOTA", operationType: "IMPO", shipmentMode: "SEA", state: ExternalDataValues.InTransitState),
                ShipmentRow("4", origin: "BOGOTA", operationType: "IMPO", shipmentMode: "AIR", state: ExternalDataValues.PendingState),
                ShipmentRow("5", origin: "CALI", operationType: "IMPO", shipmentMode: "AIR", state: ExternalDataValues.InTransitState));
            var filters = new MyShipmentsFiltersDto { ValueFilter = "bogota", OperationType = "IMPO", ShipmentMode = "AIR", State = ExternalDataValues.InTransitState };

            MyShipmentsDomainResult result = _sut.GetFiltersShipments(dataSet, ClientId, null, 1, 10, filters);

            result.MyShipments.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void GetFiltersShipments_LosTotalesSeCalculanSobreLosRegistrosFiltradosYNoSobreLaPagina()
        {
            var dataSet = Set(
                ShipmentRow("1", operationType: "IMPO", shipmentMode: "AIR", origin: "BOGOTA"),
                ShipmentRow("2", operationType: "EXPO", shipmentMode: "SEA", origin: "BOGOTA"),
                ShipmentRow("3", operationType: "IMPO", shipmentMode: "SEA", origin: "CALI"));

            MyShipmentsDomainResult result = _sut.GetFiltersShipments(dataSet, ClientId, null, 1, 1, new MyShipmentsFiltersDto { ValueFilter = "BOGOTA" });

            result.MyShipments.Should().HaveCount(1);
            result.ClientSummaryResponse.TotalClientRecords.Should().Be(2);
            result.ClientSummaryResponse.TotalImports.Should().Be(1);
            result.ClientSummaryResponse.TotalExports.Should().Be(1);
            result.ClientSummaryResponse.TotalAirShipments.Should().Be(1);
            result.ClientSummaryResponse.TotalOceanShipments.Should().Be(1);
        }

        [Fact]
        public void GetFiltersShipments_SinCoincidencias_RetornaVacio()
        {
            MyShipmentsDomainResult result = _sut.GetFiltersShipments(Set(ShipmentRow("1")), ClientId, null, 1, 10, new MyShipmentsFiltersDto { ValueFilter = "zzz" });

            result.MyShipments.Should().BeEmpty();
            result.ClientSummaryResponse.TotalClientRecords.Should().Be(0);
        }

        [Fact]
        public void GetFiltersShipments_Paginacion_AplicaDespuesDeFiltrar()
        {
            var dataSet = Set(Enumerable.Range(1, 6).Select(i => ShipmentRow(i.ToString(), operationType: i % 2 == 0 ? "EXPO" : "IMPO")).ToArray());

            MyShipmentsDomainResult result = _sut.GetFiltersShipments(dataSet, ClientId, null, 2, 2, new MyShipmentsFiltersDto { OperationType = "EXPO" });

            result.MyShipments.Select(x => x.Id).Should().Equal(6);
            result.ClientSummaryResponse.TotalClientRecords.Should().Be(3);
        }

        // ---------- GetFiltersHistoryShipments ----------

        [Theory]
        [InlineData("hbl-2", 2)]
        [InlineData("ACME", 3)]
        [InlineData("medell", 4)]
        [InlineData("miami", 5)]
        public void GetFiltersHistoryShipments_ValueFilter_CoincideEnCadaUnoDeLosCuatroCampos(String value, Int64 expectedId)
        {
            var dataSet = Set(
                ShipmentRow("1", origin: "BOG", destination: "MIA", clientName: "Otro"),
                ShipmentRow("2", origin: "BOG", destination: "MIA", clientName: "Otro"),
                ShipmentRow("3", origin: "BOG", destination: "MIA", clientName: "Acme SAS"),
                ShipmentRow("4", origin: "Medellin", destination: "MIA", clientName: "Otro"),
                ShipmentRow("5", origin: "BOG", destination: "Miami", clientName: "Otro"));

            MyShipmentsDomainResult result = _sut.GetFiltersHistoryShipments(dataSet, ClientId, null, 1, 10, new MyShipmentsFiltersDto { ValueFilter = value });

            result.MyShipments.Select(x => x.Id).Should().Contain(expectedId);
        }

        [Fact]
        public void GetFiltersHistoryShipments_SinFiltros_RetornaTodosConTotales()
        {
            var dataSet = Set(
                ShipmentRow("1", operationType: "IMPO", shipmentMode: "AIR"),
                ShipmentRow("2", operationType: "EXPO", shipmentMode: "SEA"),
                ShipmentRow("3", operationType: "EXPO", shipmentMode: "SEA"));

            MyShipmentsDomainResult result = _sut.GetFiltersHistoryShipments(dataSet, ClientId, null, 1, 2, new MyShipmentsFiltersDto());

            result.MyShipments.Select(x => x.Id).Should().Equal(1, 2);
            result.ClientSummaryResponse.TotalClientRecords.Should().Be(3);
            result.ClientSummaryResponse.TotalImports.Should().Be(1);
            result.ClientSummaryResponse.TotalExports.Should().Be(2);
            result.ClientSummaryResponse.TotalAirShipments.Should().Be(1);
            result.ClientSummaryResponse.TotalOceanShipments.Should().Be(2);
        }

        [Fact]
        public void GetFiltersHistoryShipments_FiltrosEnBlanco_SeIgnoran()
        {
            var dataSet = Set(ShipmentRow("1"), ShipmentRow("2"));
            var filters = new MyShipmentsFiltersDto { ValueFilter = " ", OperationType = " ", ShipmentMode = " " };

            _sut.GetFiltersHistoryShipments(dataSet, ClientId, null, 1, 10, filters).MyShipments.Should().HaveCount(2);
        }

        [Fact]
        public void GetFiltersHistoryShipments_SoloOperationType_Filtra()
        {
            var dataSet = Set(ShipmentRow("1", operationType: "IMPO"), ShipmentRow("2", operationType: "EXPO"));

            MyShipmentsDomainResult result = _sut.GetFiltersHistoryShipments(dataSet, ClientId, null, 1, 10, new MyShipmentsFiltersDto { OperationType = "EXPO" });

            result.MyShipments.Select(x => x.Id).Should().Equal(2);
        }

        [Fact]
        public void GetFiltersHistoryShipments_SoloShipmentMode_Filtra()
        {
            var dataSet = Set(ShipmentRow("1", shipmentMode: "AIR"), ShipmentRow("2", shipmentMode: "SEA"));

            MyShipmentsDomainResult result = _sut.GetFiltersHistoryShipments(dataSet, ClientId, null, 1, 10, new MyShipmentsFiltersDto { ShipmentMode = "SEA" });

            result.MyShipments.Select(x => x.Id).Should().Equal(2);
        }

        // ---------- GetDetailsShipments ----------

        [Fact]
        public void GetDetailsShipments_MapeaTodosLosCamposDeCadaSeccion()
        {
            var row = Row(
                (ExternalDataFields.ClientNit, ClientId),
                (ExternalDataFields.DocumentNumber, "HBL-1"),
                (ExternalDataFields.ID, "11"),
                (ExternalDataFields.ClientName, "ACME"),
                (ExternalDataFields.Supplier, "Prov"),
                (ExternalDataFields.Carrier, "Carrier"),
                (ExternalDataFields.MerchandiseDescription, "Merca"),
                (ExternalDataFields.DocumentType, "HBL"),
                (ExternalDataFields.Origin, "Orig"),
                (ExternalDataFields.Destination, "Dest"),
                (ExternalDataFields.LoadType, "FCL"),
                (ExternalDataFields.PackagesNumbers, "5"),
                (ExternalDataFields.WeightKg, "10.5"),
                (ExternalDataFields.VolumeM3, "2"),
                (ExternalDataFields.Incoterm, "FOB"),
                (ExternalDataFields.OperationType, "IMPO"),
                (ExternalDataFields.ShipmentMode, "SEA"),
                (ExternalDataFields.State, "En tránsito"),
                (ExternalDataFields.StoreOriginDate, "01/01/2024"),
                (ExternalDataFields.ETDDate, "02/01/2024"),
                (ExternalDataFields.ATDDate, "03/01/2024"),
                (ExternalDataFields.ETADate, "04/01/2024"),
                (ExternalDataFields.ATADate, "05/01/2024"),
                (ExternalDataFields.StoreDestinationDate, "06/01/2024"),
                (ExternalDataFields.NationalizationDate, "07/01/2024"),
                (ExternalDataFields.DispatchDestinationDate, "08/01/2024"),
                (ExternalDataFields.FormDate, "09/01/2024"),
                (ExternalDataFields.ContainerDeliveryDate, "10/01/2024"),
                (ExternalDataFields.ContainerType, "40HC"),
                (ExternalDataFields.ContainerAmount, "2"),
                (ExternalDataFields.ContainerNumber, "MSKU1"),
                (ExternalDataFields.DaysOff, "7"),
                (ExternalDataFields.DaysRemainingDelivery, "3"),
                (ExternalDataFields.ActualContainerReturnDate, "11/01/2024"),
                (ExternalDataFields.ContainerDelayDays, "1"),
                (ExternalDataFields.CostDayOfDelay, "100"),
                (ExternalDataFields.TotalCostContainerDelays, "100"),
                (ExternalDataFields.ContainerDepot, "500"),
                (ExternalDataFields.AdvancePaymentRequestDate, "12/01/2024"),
                (ExternalDataFields.AdvancePaymentDate, "13/01/2024"),
                (ExternalDataFields.AdvancePaymentAmount, "1000"),
                (ExternalDataFields.SupplierInvoice, "FP"),
                (ExternalDataFields.TCCInvoice, "FT"),
                (ExternalDataFields.InvoiceNumber, "NF"),
                (ExternalDataFields.InvoiceDate, "14/01/2024"),
                (ExternalDataFields.ExpenseDescription, "Flete"),
                (ExternalDataFields.ExpenseAmountUSD, "20"),
                (ExternalDataFields.InvoiceSubtotalUSD, "30"),
                (ExternalDataFields.IvaUSD, "5.7"),
                (ExternalDataFields.TotalInvoiceUSD, "35.7"));

            DetailsShipmentsDomainDtoResult r = _sut.GetDetailsShipments(Set(row), ClientId, null, "HBL-1");

            r.ResumenShipments!.Id.Should().Be("11");
            r.ResumenShipments.ClientNit.Should().Be(ClientId);
            r.ResumenShipments.ClientName.Should().Be("ACME");
            r.ResumenShipments.Supplier.Should().Be("Prov");
            r.ResumenShipments.Carrier.Should().Be("Carrier");
            r.ResumenShipments.MerchandiseDescription.Should().Be("Merca");
            r.ResumenShipments.DocumentNumber.Should().Be("HBL-1");
            r.ResumenShipments.DocumentType.Should().Be("HBL");
            r.ResumenShipments.Origin.Should().Be("Orig");
            r.ResumenShipments.Destination.Should().Be("Dest");
            r.ResumenShipments.LoadType.Should().Be("FCL");
            r.ResumenShipments.PackagesNumbers.Should().Be("5");
            r.ResumenShipments.WeightKg.Should().Be("10.5");
            r.ResumenShipments.VolumeM3.Should().Be("2");
            r.ResumenShipments.Incoterm.Should().Be("FOB");
            r.ResumenShipments.OperationType.Should().Be("IMPO");
            r.ResumenShipments.ShipmentMode.Should().Be("SEA");
            r.TrackingShipments!.State.Should().Be("En tránsito");

            r.LogisticsDatesShipments!.StoreOriginDate.Should().Be(new DateTime(2024, 1, 1));
            r.LogisticsDatesShipments.ETDDate.Should().Be(new DateTime(2024, 1, 2));
            r.LogisticsDatesShipments.ATDDate.Should().Be(new DateTime(2024, 1, 3));
            r.LogisticsDatesShipments.ETADate.Should().Be(new DateTime(2024, 1, 4));
            r.LogisticsDatesShipments.ATADate.Should().Be(new DateTime(2024, 1, 5));
            r.LogisticsDatesShipments.StoreDestinationDate.Should().Be(new DateTime(2024, 1, 6));
            r.LogisticsDatesShipments.NationalizationDate.Should().Be(new DateTime(2024, 1, 7));
            r.LogisticsDatesShipments.DispatchDestinationDate.Should().Be(new DateTime(2024, 1, 8));
            r.LogisticsDatesShipments.FormDate.Should().Be(new DateTime(2024, 1, 9));
            r.LogisticsDatesShipments.ContainerDeliveryDate.Should().Be(new DateTime(2024, 1, 10));

            r.ContainerShipments!.ContainerType.Should().Be("40HC");
            r.ContainerShipments.ContainerAmount.Should().Be("2");
            r.ContainerShipments.ContainerNumber.Should().Be("MSKU1");
            r.ContainerShipments.DaysOff.Should().Be("7");
            r.ContainerShipments.DaysRemainingDelivery.Should().Be("3");
            r.ContainerShipments.ActualContainerReturnDate.Should().Be(new DateTime(2024, 1, 11));
            r.ContainerShipments.ContainerDelayDays.Should().Be("1");
            r.ContainerShipments.CostDayOfDelay.Should().Be("100");
            r.ContainerShipments.TotalCostContainerDelays.Should().Be("100");
            r.ContainerShipments.ContainerDepot.Should().Be("500");

            r.FinancialInfoShipments!.AdvancePaymentRequestDate.Should().Be(new DateTime(2024, 1, 12));
            r.FinancialInfoShipments.AdvancePaymentDate.Should().Be(new DateTime(2024, 1, 13));
            r.FinancialInfoShipments.AdvancePaymentAmount.Should().Be("1000");
            r.FinancialInfoShipments.SupplierInvoice.Should().Be("FP");
            r.FinancialInfoShipments.TCCInvoice.Should().Be("FT");
            r.FinancialInfoShipments.InvoiceNumber.Should().Be("NF");
            r.FinancialInfoShipments.InvoiceDate.Should().Be("14/01/2024");
            r.FinancialInfoShipments.ExpenseDescription.Should().Be("Flete");
            r.FinancialInfoShipments.ExpenseAmountUSD.Should().Be("20");
            r.FinancialInfoShipments.InvoiceSubtotalUSD.Should().Be("30");
            r.FinancialInfoShipments.IvaUSD.Should().Be("5.7");
            r.FinancialInfoShipments.TotalInvoiceUSD.Should().Be("35.7");
        }

        [Fact]
        public void GetDetailsShipments_ConCamposFaltantes_RetornaCadenasVaciasYFechasMinimas()
        {
            var row = Row((ExternalDataFields.ClientNit, ClientId), (ExternalDataFields.DocumentNumber, "HBL-1"));

            DetailsShipmentsDomainDtoResult r = _sut.GetDetailsShipments(Set(row), ClientId, null, "HBL-1");

            r.ResumenShipments!.Supplier.Should().BeEmpty();
            r.LogisticsDatesShipments!.ETDDate.Should().Be(DateTime.MinValue);
            r.ContainerShipments!.ActualContainerReturnDate.Should().Be(DateTime.MinValue);
            r.FinancialInfoShipments!.AdvancePaymentDate.Should().Be(DateTime.MinValue);
        }

        [Fact]
        public void GetDetailsShipments_ConDocumentosRepetidos_RetornaElPrimero()
        {
            var dataSet = Set(
                Row((ExternalDataFields.ClientNit, ClientId), (ExternalDataFields.DocumentNumber, "HBL-1"), (ExternalDataFields.ClientName, "Primero")),
                Row((ExternalDataFields.ClientNit, ClientId), (ExternalDataFields.DocumentNumber, "HBL-1"), (ExternalDataFields.ClientName, "Segundo")));

            _sut.GetDetailsShipments(dataSet, ClientId, null, "HBL-1").ResumenShipments!.ClientName.Should().Be("Primero");
        }

        [Fact]
        public void GetDetailsShipments_ElDocumentoSeCompararConIgualdadExacta()
        {
            var dataSet = Set(Row((ExternalDataFields.ClientNit, ClientId), (ExternalDataFields.DocumentNumber, "HBL-1")));

            Action act = () => _sut.GetDetailsShipments(dataSet, ClientId, null, "hbl-1");

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GetDetailsShipments_SinRegistros_LanzaArgumentException()
        {
            Action act = () => _sut.GetDetailsShipments(Set(), ClientId, null, "HBL-1");

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GetDetailsShipments_ConClientIdVacio_BuscaEnTodosLosRegistros()
        {
            var dataSet = Set(Row((ExternalDataFields.ClientNit, "OTRO"), (ExternalDataFields.DocumentNumber, "HBL-9"), (ExternalDataFields.ClientName, "Otro cliente")));

            DetailsShipmentsDomainDtoResult r = _sut.GetDetailsShipments(dataSet, String.Empty, null, "HBL-9");

            r.ResumenShipments!.ClientName.Should().Be("Otro cliente");
        }
    }
}
