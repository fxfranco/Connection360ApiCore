using Connection360.Domain.Constans;
using Connection360.Domain.Dtos;
using Connection360.Domain.Entities;
using Connection360.Domain.Services;
using FluentAssertions;
using Xunit;
using static Connection360.Domain.Tests.TestSupport.DynamicDataSetBuilder;

namespace Connection360.Domain.Tests.Services
{
    public class MyShipmentsDomainServiceTests
    {
        private readonly MyShipmentsDomainService _sut = new(new ClientRecordsFilterService());
        private const String ClientId = "900123456";

        private static DynamicRecord ShipmentRow(String id, String state, String operationType = "IMPO", String shipmentMode = "AIR",
            String clientNit = ClientId, String clientName = "Cliente Demo", String origin = "BOG", String destination = "MIA",
            String creationDate = "01/01/2024")
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
                (ExternalDataFields.DocumentNumber, $"HBL-{id}"),
                (ExternalDataFields.CreationDate, creationDate));
        }

        [Fact]
        public void GetAllShipments_ExcluyeRegistrosEnEstadoEntregado()
        {
            var dataSet = DataSet(new[] { ExternalDataFields.ClientNit },
                ShipmentRow("1", ExternalDataValues.PendingState),
                ShipmentRow("2", ExternalDataValues.DeliveredState));

            MyShipmentsDomainResult result = _sut.GetAllShipments(dataSet, ClientId, null, page: 1, size: 10);

            result.MyShipments.Should().ContainSingle(x => x.Id == 1);
        }

        [Fact]
        public void GetAllShipments_FiltraPorCliente()
        {
            var dataSet = DataSet(new[] { ExternalDataFields.ClientNit },
                ShipmentRow("1", ExternalDataValues.PendingState, clientNit: ClientId),
                ShipmentRow("2", ExternalDataValues.PendingState, clientNit: "OTRO-CLIENTE"));

            MyShipmentsDomainResult result = _sut.GetAllShipments(dataSet, ClientId, null, page: 1, size: 10);

            result.MyShipments.Should().ContainSingle();
            result.ClientSummaryResponse.TotalClientRecords.Should().Be(1);
        }

        [Fact]
        public void GetAllShipments_AplicaPaginacionConSkipYTake()
        {
            var rows = Enumerable.Range(1, 5).Select(i => ShipmentRow(i.ToString(), ExternalDataValues.PendingState)).ToArray();
            var dataSet = DataSet(new[] { ExternalDataFields.ClientNit }, rows);

            MyShipmentsDomainResult page1 = _sut.GetAllShipments(dataSet, ClientId, null, page: 1, size: 2);
            MyShipmentsDomainResult page2 = _sut.GetAllShipments(dataSet, ClientId, null, page: 2, size: 2);

            page1.MyShipments.Should().HaveCount(2);
            page2.MyShipments.Should().HaveCount(2);
            page1.MyShipments.Select(x => x.Id).Should().NotIntersectWith(page2.MyShipments.Select(x => x.Id));
        }

        [Fact]
        public void GetAllShipments_CalculaTotalesDeResumenCorrectamente()
        {
            var dataSet = DataSet(new[] { ExternalDataFields.ClientNit },
                ShipmentRow("1", ExternalDataValues.PendingState, operationType: ExternalDataValues.Import, shipmentMode: ExternalDataValues.AirShipment),
                ShipmentRow("2", ExternalDataValues.PendingState, operationType: ExternalDataValues.Export, shipmentMode: ExternalDataValues.OceanShipment));

            MyShipmentsDomainResult result = _sut.GetAllShipments(dataSet, ClientId, null, page: 1, size: 10);

            result.ClientSummaryResponse.TotalImports.Should().Be(1);
            result.ClientSummaryResponse.TotalExports.Should().Be(1);
            result.ClientSummaryResponse.TotalAirShipments.Should().Be(1);
            result.ClientSummaryResponse.TotalOceanShipments.Should().Be(1);
        }

        [Fact]
        public void GetHistoryAllShipments_IncluyeSoloRegistrosEntregados()
        {
            var dataSet = DataSet(new[] { ExternalDataFields.ClientNit },
                ShipmentRow("1", ExternalDataValues.DeliveredState),
                ShipmentRow("2", ExternalDataValues.PendingState));

            MyShipmentsDomainResult result = _sut.GetHistoryAllShipments(dataSet, ClientId, null, page: 1, size: 10);

            result.MyShipments.Should().ContainSingle(x => x.Id == 1);
        }

        [Fact]
        public void GetFiltersShipments_ConValueFilter_BuscaEnDocumentoClienteOrigenYDestino()
        {
            var dataSet = DataSet(new[] { ExternalDataFields.ClientNit },
                ShipmentRow("1", ExternalDataValues.PendingState, origin: "BOGOTA"),
                ShipmentRow("2", ExternalDataValues.PendingState, origin: "MEDELLIN"));

            var filters = new MyShipmentsFiltersDto { ValueFilter = "bogota" };

            MyShipmentsDomainResult result = _sut.GetFiltersShipments(dataSet, ClientId, null, page: 1, size: 10, filters);

            result.MyShipments.Should().ContainSingle(x => x.Id == 1);
        }

        [Fact]
        public void GetFiltersShipments_ConFiltroDeOperationType_FiltraCorrectamente()
        {
            var dataSet = DataSet(new[] { ExternalDataFields.ClientNit },
                ShipmentRow("1", ExternalDataValues.PendingState, operationType: ExternalDataValues.Import),
                ShipmentRow("2", ExternalDataValues.PendingState, operationType: ExternalDataValues.Export));

            var filters = new MyShipmentsFiltersDto { OperationType = ExternalDataValues.Export };

            MyShipmentsDomainResult result = _sut.GetFiltersShipments(dataSet, ClientId, null, page: 1, size: 10, filters);

            result.MyShipments.Should().ContainSingle(x => x.Id == 2);
        }

        [Fact]
        public void GetFiltersShipments_ConFiltroDeShipmentMode_FiltraCorrectamente()
        {
            var dataSet = DataSet(new[] { ExternalDataFields.ClientNit },
                ShipmentRow("1", ExternalDataValues.PendingState, shipmentMode: ExternalDataValues.AirShipment),
                ShipmentRow("2", ExternalDataValues.PendingState, shipmentMode: ExternalDataValues.OceanShipment));

            var filters = new MyShipmentsFiltersDto { ShipmentMode = ExternalDataValues.OceanShipment };

            MyShipmentsDomainResult result = _sut.GetFiltersShipments(dataSet, ClientId, null, page: 1, size: 10, filters);

            result.MyShipments.Should().ContainSingle(x => x.Id == 2);
        }

        [Fact]
        public void GetFiltersShipments_ConFiltroDeState_FiltraCorrectamente()
        {
            var dataSet = DataSet(new[] { ExternalDataFields.ClientNit },
                ShipmentRow("1", ExternalDataValues.PendingState),
                ShipmentRow("2", ExternalDataValues.InTransitState));

            var filters = new MyShipmentsFiltersDto { State = ExternalDataValues.InTransitState };

            MyShipmentsDomainResult result = _sut.GetFiltersShipments(dataSet, ClientId, null, page: 1, size: 10, filters);

            result.MyShipments.Should().ContainSingle(x => x.Id == 2);
        }

        [Fact]
        public void GetFiltersShipments_SinFiltros_RetornaTodosLosNoEntregados()
        {
            var dataSet = DataSet(new[] { ExternalDataFields.ClientNit },
                ShipmentRow("1", ExternalDataValues.PendingState),
                ShipmentRow("2", ExternalDataValues.InTransitState),
                ShipmentRow("3", ExternalDataValues.DeliveredState));

            var filters = new MyShipmentsFiltersDto();

            MyShipmentsDomainResult result = _sut.GetFiltersShipments(dataSet, ClientId, null, page: 1, size: 10, filters);

            result.MyShipments.Should().HaveCount(2);
        }

        [Fact]
        public void GetFiltersHistoryShipments_CombinaEstadoEntregadoConFiltroDeValueFilter()
        {
            var dataSet = DataSet(new[] { ExternalDataFields.ClientNit },
                ShipmentRow("1", ExternalDataValues.DeliveredState, origin: "BOGOTA"),
                ShipmentRow("2", ExternalDataValues.DeliveredState, origin: "MEDELLIN"),
                ShipmentRow("3", ExternalDataValues.PendingState, origin: "BOGOTA"));

            var filters = new MyShipmentsFiltersDto { ValueFilter = "bogota" };

            MyShipmentsDomainResult result = _sut.GetFiltersHistoryShipments(dataSet, ClientId, null, page: 1, size: 10, filters);

            result.MyShipments.Should().ContainSingle(x => x.Id == 1);
        }

        [Fact]
        public void GetDetailsShipments_MapeaTodasLasSeccionesDelDetalle()
        {
            var row = Row(
                (ExternalDataFields.ClientNit, ClientId),
                (ExternalDataFields.DocumentNumber, "HBL-001"),
                (ExternalDataFields.ID, "1"),
                (ExternalDataFields.ClientName, "Cliente Demo"),
                (ExternalDataFields.Supplier, "Proveedor X"),
                (ExternalDataFields.Carrier, "Transportista Y"),
                (ExternalDataFields.State, ExternalDataValues.InTransitState),
                (ExternalDataFields.ContainerType, "40HC"),
                (ExternalDataFields.AdvancePaymentAmount, "1000"));

            var dataSet = DataSet(new[] { ExternalDataFields.ClientNit }, row);

            DetailsShipmentsDomainDtoResult result = _sut.GetDetailsShipments(dataSet, ClientId, null, "HBL-001");

            result.ResumenShipments!.ClientName.Should().Be("Cliente Demo");
            result.ResumenShipments.Supplier.Should().Be("Proveedor X");
            result.TrackingShipments!.State.Should().Be(ExternalDataValues.InTransitState);
            result.ContainerShipments!.ContainerType.Should().Be("40HC");
            result.FinancialInfoShipments!.AdvancePaymentAmount.Should().Be("1000");
        }

        [Fact]
        public void GetDetailsShipments_SinCoincidencias_LanzaInvalidOperationException()
        {
            var dataSet = DataSet(new[] { ExternalDataFields.ClientNit },
                Row((ExternalDataFields.ClientNit, "OTRO-CLIENTE"), (ExternalDataFields.DocumentNumber, "HBL-999")));

            Action act = () => _sut.GetDetailsShipments(dataSet, ClientId, null, "HBL-001");

            // First() sin resultados lanza InvalidOperationException (comportamiento actual del servicio)
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void GetAllShipments_ConClientesDeColaborador_IncluyeLosEnviosDeTodosSusClientes()
        {
            var dataSet = DataSet(new[] { ExternalDataFields.ClientNit },
                ShipmentRow("1", ExternalDataValues.PendingState, clientNit: ClientId),
                ShipmentRow("2", ExternalDataValues.PendingState, clientNit: "800111222"),
                ShipmentRow("3", ExternalDataValues.PendingState, clientNit: "NO-ASIGNADO"));

            var customers = BuildCustomers(ClientId, "800111222");

            MyShipmentsDomainResult result = _sut.GetAllShipments(dataSet, "COLABORADOR-1", customers, page: 1, size: 10);

            result.MyShipments.Select(x => x.Id).Should().BeEquivalentTo(new Int64[] { 1, 2 });
            result.ClientSummaryResponse.TotalClientRecords.Should().Be(2);
        }

        [Fact]
        public void GetFiltersHistoryShipments_ConFiltrosDeOperationTypeYShipmentMode_FiltraCorrectamente()
        {
            var dataSet = DataSet(new[] { ExternalDataFields.ClientNit },
                ShipmentRow("1", ExternalDataValues.DeliveredState, operationType: ExternalDataValues.Import, shipmentMode: ExternalDataValues.AirShipment),
                ShipmentRow("2", ExternalDataValues.DeliveredState, operationType: ExternalDataValues.Export, shipmentMode: ExternalDataValues.AirShipment),
                ShipmentRow("3", ExternalDataValues.DeliveredState, operationType: ExternalDataValues.Export, shipmentMode: ExternalDataValues.OceanShipment));

            var filters = new MyShipmentsFiltersDto { OperationType = ExternalDataValues.Export, ShipmentMode = ExternalDataValues.OceanShipment };

            MyShipmentsDomainResult result = _sut.GetFiltersHistoryShipments(dataSet, ClientId, null, page: 1, size: 10, filters);

            result.MyShipments.Should().ContainSingle(x => x.Id == 3);
        }

        [Fact]
        public void GetDetailsShipments_ConClientesDeColaborador_EncuentraElDocumentoDeUnClienteAsignado()
        {
            var dataSet = DataSet(new[] { ExternalDataFields.ClientNit },
                Row((ExternalDataFields.ClientNit, "800111222"), (ExternalDataFields.DocumentNumber, "HBL-777"), (ExternalDataFields.ClientName, "Cliente Dos")));

            var customers = BuildCustomers(ClientId, "800111222");

            DetailsShipmentsDomainDtoResult result = _sut.GetDetailsShipments(dataSet, "COLABORADOR-1", customers, "HBL-777");

            result.ResumenShipments!.ClientName.Should().Be("Cliente Dos");
        }

        private static List<CustomersOfCollaboratorDtoResult> BuildCustomers(params String[] customerNits)
        {
            return customerNits
                .Select((nit, index) => new CustomersOfCollaboratorDtoResult
                {
                    IdCollaborator = 1,
                    IdentificacionCollaborator = "COLABORADOR-1",
                    IdCustomer = index + 1,
                    IdentificacionCustomer = nit
                })
                .ToList();
        }
    }
}
