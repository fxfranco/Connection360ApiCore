using Connection360.Domain.Constans;
using Connection360.Domain.Dtos;
using Connection360.Domain.Entities;
using Connection360.Domain.Enum;
using Connection360.Domain.Services;
using FluentAssertions;
using Xunit;
using static Connection360.Domain.Tests.TestSupport.DynamicDataSetBuilder;

namespace Connection360.Domain.Tests.Services
{
    /// <summary>Casos complementarios de ClientRecordsFilterService, ClientSummaryDomainService, ReportsDomainService,
    /// DynamicDataSetMerger y DetailsHistoryShipmentsDomainService.</summary>
    public class DomainServicesComplementTests
    {
        private const String ClientId = "900123456";
        private static readonly String[] Fields = { ExternalDataFields.ClientNit };

        private static List<CustomersOfCollaboratorDtoResult> Customers(params String?[] nits) =>
            nits.Select((n, i) => new CustomersOfCollaboratorDtoResult { IdCustomer = i, IdentificacionCustomer = n! }).ToList();

        // ---------- ClientRecordsFilterService ----------

        [Fact]
        public void ClientRecordsFilter_ConClientIdVacio_IgnoraLosClientesDelColaborador()
        {
            var sut = new ClientRecordsFilterService();
            var dataSet = DataSet(Fields, Row((ExternalDataFields.ClientNit, "A")), Row((ExternalDataFields.ClientNit, "B")));

            List<DynamicRecord> result = sut.Filter(dataSet, String.Empty, Customers("A"));

            result.Should().HaveCount(2);
        }

        [Fact]
        public void ClientRecordsFilter_ConDataSetVacio_RetornaListaVacia()
        {
            var sut = new ClientRecordsFilterService();

            sut.Filter(DataSet(Fields), ClientId, null).Should().BeEmpty();
            sut.Filter(DataSet(Fields), String.Empty, null).Should().BeEmpty();
        }

        [Fact]
        public void ClientRecordsFilter_ConClientesDeColaborador_NoIncluyeElClientIdDelColaborador()
        {
            var sut = new ClientRecordsFilterService();
            var dataSet = DataSet(Fields, Row((ExternalDataFields.ClientNit, "COLABORADOR")), Row((ExternalDataFields.ClientNit, "A")));

            List<DynamicRecord> result = sut.Filter(dataSet, "COLABORADOR", Customers("A"));

            result.Should().ContainSingle().Which[ExternalDataFields.ClientNit].Should().Be("A");
        }

        [Fact]
        public void ClientRecordsFilter_RegistroSinNit_NoCoincideConNingunCliente()
        {
            var sut = new ClientRecordsFilterService();
            var dataSet = DataSet(Fields, Row((ExternalDataFields.State, "x")));

            sut.Filter(dataSet, ClientId, null).Should().BeEmpty();
        }

        [Fact]
        public void ClientRecordsFilter_ConservaElOrdenOriginal()
        {
            var sut = new ClientRecordsFilterService();
            var dataSet = DataSet(Fields,
                Row((ExternalDataFields.ClientNit, "A"), (ExternalDataFields.ID, "3")),
                Row((ExternalDataFields.ClientNit, "A"), (ExternalDataFields.ID, "1")),
                Row((ExternalDataFields.ClientNit, "A"), (ExternalDataFields.ID, "2")));

            sut.Filter(dataSet, "A", null).Select(r => r[ExternalDataFields.ID]).Should().Equal("3", "1", "2");
        }

        [Fact]
        public void ClientRecordsFilter_SinClientId_RetornaUnaCopiaDeLasFilas()
        {
            var sut = new ClientRecordsFilterService();
            var dataSet = DataSet(Fields, Row((ExternalDataFields.ClientNit, "A")));

            List<DynamicRecord> result = sut.Filter(dataSet, String.Empty, null);
            result.Clear();

            dataSet.Rows.Should().HaveCount(1);
        }

        // ---------- ClientSummaryDomainService ----------

        private static DynamicRecord Shipment(String id, String creation, String state = "Pendiente", String op = "IMPO", String mode = "AIR", String nit = ClientId, String doc = "")
        {
            return Row(
                (ExternalDataFields.ID, id), (ExternalDataFields.ClientNit, nit), (ExternalDataFields.CreationDate, creation),
                (ExternalDataFields.State, state), (ExternalDataFields.OperationType, op), (ExternalDataFields.ShipmentMode, mode),
                (ExternalDataFields.DocumentNumber, String.IsNullOrEmpty(doc) ? $"HBL-{id}" : doc), (ExternalDataFields.ClientName, "Cliente"),
                (ExternalDataFields.Origin, "O"), (ExternalDataFields.Destination, "D"));
        }

        [Fact]
        public void ClientSummary_Summarize_ConLastRecordsCountCero_NoRetornaEnviosRecientes()
        {
            var sut = new ClientSummaryDomainService(new ClientRecordsFilterService());
            var dataSet = DataSet(Fields, Shipment("1", "01/01/2024"), Shipment("2", "01/01/2025"));

            ClientSummaryDomainResult result = sut.Summarize(dataSet, ClientId, null, 0);

            result.RecentShipments.Should().BeEmpty();
            result.TotalClientRecords.Should().Be(2);
        }

        [Fact]
        public void ClientSummary_Summarize_CuentaSoloEstadoConNovedadComoIssues()
        {
            var sut = new ClientSummaryDomainService(new ClientRecordsFilterService());
            var dataSet = DataSet(Fields,
                Shipment("1", "01/01/2024", state: ExternalDataValues.WithIssuesState),
                Shipment("2", "01/01/2024", state: ExternalDataValues.WithIssuesState),
                Shipment("3", "01/01/2024", state: ExternalDataValues.DeliveredState));

            sut.Summarize(dataSet, ClientId, null, 10).TotalWithIssues.Should().Be(2);
        }

        [Fact]
        public void ClientSummary_Summarize_ConFechasInvalidas_LasOrdenaAlFinal()
        {
            var sut = new ClientSummaryDomainService(new ClientRecordsFilterService());
            var dataSet = DataSet(Fields,
                Shipment("1", "no-es-fecha"),
                Shipment("2", "01/01/2025"),
                Shipment("3", "01/01/2024"));

            ClientSummaryDomainResult result = sut.Summarize(dataSet, ClientId, null, 10);

            result.RecentShipments.Select(x => x.Id).Should().Equal(2, 3, 1);
        }

        [Fact]
        public void ClientSummary_Filter_ConDocumentoNulo_RetornaDtoVacio()
        {
            var sut = new ClientSummaryDomainService(new ClientRecordsFilterService());
            var dataSet = DataSet(Fields, Shipment("1", "01/01/2024"));

            ResumenClienteDto result = sut.Filter(dataSet, ClientId, null, null!);

            result.Id.Should().Be(0);
            result.DocumentNumber.Should().BeEmpty();
        }

        [Fact]
        public void ClientSummary_Filter_ConDocumentosRepetidos_RetornaElPrimero()
        {
            var sut = new ClientSummaryDomainService(new ClientRecordsFilterService());
            var dataSet = DataSet(Fields, Shipment("1", "01/01/2024", doc: "HBL-X"), Shipment("2", "01/01/2024", doc: "HBL-X"));

            sut.Filter(dataSet, ClientId, null, "HBL-X").Id.Should().Be(1);
        }

        [Fact]
        public void ClientSummary_Filter_MapeaTodosLosCampos()
        {
            var sut = new ClientSummaryDomainService(new ClientRecordsFilterService());
            var row = Row(
                (ExternalDataFields.ID, "9"), (ExternalDataFields.ClientNit, ClientId), (ExternalDataFields.ClientName, "ACME"),
                (ExternalDataFields.DocumentNumber, "HBL-9"), (ExternalDataFields.Origin, "BOG"), (ExternalDataFields.Destination, "MIA"),
                (ExternalDataFields.State, "En tránsito"), (ExternalDataFields.OperationType, "EXPO"), (ExternalDataFields.ShipmentMode, "SEA"));

            ResumenClienteDto result = sut.Filter(DataSet(Fields, row), ClientId, null, "hbl-9");

            result.Id.Should().Be(9);
            result.ClientNit.Should().Be(ClientId);
            result.ClientName.Should().Be("ACME");
            result.DocumentNumber.Should().Be("HBL-9");
            result.Origin.Should().Be("BOG");
            result.Destination.Should().Be("MIA");
            result.Status.Should().Be("En tránsito");
            result.OperationType.Should().Be("EXPO");
            result.ShipmentMode.Should().Be("SEA");
        }

        [Fact]
        public void ClientSummary_Filter_ConIdNoNumerico_MapeaIdCero()
        {
            var sut = new ClientSummaryDomainService(new ClientRecordsFilterService());
            var dataSet = DataSet(Fields, Shipment("abc", "01/01/2024", doc: "HBL-Z"));

            sut.Filter(dataSet, ClientId, null, "HBL-Z").Id.Should().Be(0);
        }

        // ---------- ReportsDomainService ----------

        private static DynamicRecord ReportRow(String nit, String name, String state = "Pendiente", String origin = "O", String destination = "D",
            String total = "0", String advance = "0", String delay = "0", String op = "IMPO", String mode = "AIR")
        {
            return Row(
                (ExternalDataFields.ClientNit, nit), (ExternalDataFields.ClientName, name), (ExternalDataFields.State, state),
                (ExternalDataFields.Origin, origin), (ExternalDataFields.Destination, destination),
                (ExternalDataFields.TotalInvoiceUSD, total), (ExternalDataFields.AdvancePaymentAmount, advance),
                (ExternalDataFields.TotalCostContainerDelays, delay), (ExternalDataFields.OperationType, op), (ExternalDataFields.ShipmentMode, mode));
        }

        [Fact]
        public void Reports_Summarize_ConMismoNitYNombreDistinto_GeneraGruposSeparados()
        {
            var sut = new ReportsDomainService(new ClientRecordsFilterService());
            var dataSet = DataSet(Fields, ReportRow("A", "Nombre 1"), ReportRow("A", "Nombre 2"));

            List<ReportsSummaryDomainDtoResult> result = sut.Summarize(dataSet, String.Empty, 5, null);

            result.Should().HaveCount(2);
            result.Select(r => r.ClientName).Should().Equal("Nombre 1", "Nombre 2");
        }

        [Fact]
        public void Reports_Summarize_ConValoresMonetariosInvalidos_LosTrataComoCero()
        {
            var sut = new ReportsDomainService(new ClientRecordsFilterService());
            var dataSet = DataSet(Fields,
                ReportRow("A", "N", total: "1234.50", advance: "abc", delay: ""),
                ReportRow("A", "N", total: "", advance: "10.25", delay: "5"));

            ReportsSummaryDomainDtoResult result = sut.Summarize(dataSet, String.Empty, 5, null).Single();

            result.TotalInvoiced.Should().Be(1234.50);
            result.TotalAdvancePayment.Should().Be(10.25);
            result.TotalDelays.Should().Be(5);
        }

        [Fact]
        public void Reports_Summarize_FrequentRoutesCountCero_NoRetornaRutas()
        {
            var sut = new ReportsDomainService(new ClientRecordsFilterService());
            var dataSet = DataSet(Fields, ReportRow("A", "N"));

            sut.Summarize(dataSet, String.Empty, 0, null).Single().FrequentRoutes.Should().BeEmpty();
        }

        [Fact]
        public void Reports_Summarize_RutasConMismoOrigenYDestinoSeAgrupan()
        {
            var sut = new ReportsDomainService(new ClientRecordsFilterService());
            var dataSet = DataSet(Fields,
                ReportRow("A", "N", origin: "BOG", destination: "MIA"),
                ReportRow("A", "N", origin: "BOG", destination: "MIA"),
                ReportRow("A", "N", origin: "MIA", destination: "BOG"));

            List<ReportsFrequentRoutesDomainDtoResult> routes = sut.Summarize(dataSet, String.Empty, 5, null).Single().FrequentRoutes;

            routes.Should().HaveCount(2);
            routes[0].Origin.Should().Be("BOG");
            routes[0].Destination.Should().Be("MIA");
            routes[0].TotalRoute.Should().Be(2);
            routes[1].TotalRoute.Should().Be(1);
        }

        [Fact]
        public void Reports_Summarize_CuentaTodosLosEstadosYTiposPorGrupo()
        {
            var sut = new ReportsDomainService(new ClientRecordsFilterService());
            var dataSet = DataSet(Fields,
                ReportRow("A", "N", state: ExternalDataValues.WithIssuesState, op: "IMPO", mode: "AIR"),
                ReportRow("A", "N", state: ExternalDataValues.DeliveredState, op: "EXPO", mode: "SEA"),
                ReportRow("A", "N", state: ExternalDataValues.DestinationCustomsState, op: "EXPO", mode: "SEA"),
                ReportRow("A", "N", state: ExternalDataValues.OriginCustomsState),
                ReportRow("A", "N", state: ExternalDataValues.InTransitState),
                ReportRow("A", "N", state: ExternalDataValues.PendingState),
                ReportRow("A", "N", state: "Otro"));

            ReportsSummaryDomainDtoResult r = sut.Summarize(dataSet, String.Empty, 5, null).Single();

            r.TotalClientRecords.Should().Be(7);
            r.TotalWithIssuesStatus.Should().Be(1);
            r.TotalDeliveredStatus.Should().Be(1);
            r.TotalDestinationCustomsStatus.Should().Be(1);
            r.TotalOriginCustomsStatus.Should().Be(1);
            r.TotalInTransitStatus.Should().Be(1);
            r.TotalPendingStatus.Should().Be(1);
            r.TotalImports.Should().Be(5);
            r.TotalExports.Should().Be(2);
            r.TotalAirShipments.Should().Be(5);
            r.TotalOceanShipments.Should().Be(2);
        }

        // ---------- DynamicDataSetMerger ----------

        private static DynamicDataSet KeyedSet(String field, params (String Key, String Value)[] rows) =>
            DataSet(new[] { "ID", field }, rows.Select(r => Row(("ID", r.Key), (field, r.Value))).ToArray());

        [Fact]
        public void Merger_PorDefectoUsaFullOuter()
        {
            var sut = new DynamicDataSetMerger();
            var a = KeyedSet("A", ("1", "a1"));
            var b = KeyedSet("B", ("2", "b2"));

            DynamicDataSet result = sut.Merge(new[] { a, b }, "ID");

            result.Rows.Select(r => r["ID"]).Should().Equal("1", "2");
        }

        [Fact]
        public void Merger_Inner_ConTresDataSets_ConservaSoloLaInterseccionDeTodos()
        {
            var sut = new DynamicDataSetMerger();
            var a = KeyedSet("A", ("1", "a1"), ("2", "a2"), ("3", "a3"));
            var b = KeyedSet("B", ("2", "b2"), ("3", "b3"));
            var c = KeyedSet("C", ("3", "c3"), ("4", "c4"));

            DynamicDataSet result = sut.Merge(new[] { a, b, c }, "ID", DataSetJoinType.Inner);

            result.Rows.Should().ContainSingle();
            result.Rows[0]["ID"].Should().Be("3");
            result.Rows[0]["A"].Should().Be("a3");
            result.Rows[0]["B"].Should().Be("b3");
            result.Rows[0]["C"].Should().Be("c3");
        }

        [Fact]
        public void Merger_Inner_SinInterseccion_RetornaFilasVaciasConLosCamposUnidos()
        {
            var sut = new DynamicDataSetMerger();
            var a = KeyedSet("A", ("1", "a1"));
            var b = KeyedSet("B", ("2", "b2"));

            DynamicDataSet result = sut.Merge(new[] { a, b }, "ID", DataSetJoinType.Inner);

            result.Rows.Should().BeEmpty();
            result.AvailableFields.Should().Equal("ID", "A", "B");
        }

        [Fact]
        public void Merger_LasLlavesSeComparanSinDistincionDeMayusculas()
        {
            var sut = new DynamicDataSetMerger();
            var a = KeyedSet("A", ("abc", "a"));
            var b = KeyedSet("B", ("ABC", "b"));

            DynamicDataSet result = sut.Merge(new[] { a, b }, "ID", DataSetJoinType.Inner);

            result.Rows.Should().ContainSingle();
            result.Rows[0]["A"].Should().Be("a");
            result.Rows[0]["B"].Should().Be("b");
        }

        [Fact]
        public void Merger_ElPrimerValorNoVacioPrevalece()
        {
            var sut = new DynamicDataSetMerger();
            var a = KeyedSet("X", ("1", "primero"));
            var b = KeyedSet("X", ("1", "segundo"));

            sut.Merge(new[] { a, b }, "ID").Rows.Single()["X"].Should().Be("primero");
        }

        [Fact]
        public void Merger_ConDataSetsDuplicadosDeLlaveEnElMismoSet_FusionaEnUnaSolaFila()
        {
            var sut = new DynamicDataSetMerger();
            var a = DataSet(new[] { "ID", "X", "Y" }, Row(("ID", "1"), ("X", "x")), Row(("ID", "1"), ("Y", "y")));
            var b = KeyedSet("Z", ("9", "z"));

            DynamicDataSet result = sut.Merge(new[] { a, b }, "ID");

            DynamicRecord merged = result.Rows.Single(r => r["ID"] == "1");
            merged["X"].Should().Be("x");
            merged["Y"].Should().Be("y");
        }

        [Fact]
        public void Merger_ConJoinFieldNuloYUnSoloDataSet_NoLanzaExcepcion()
        {
            var sut = new DynamicDataSetMerger();
            var a = KeyedSet("A", ("1", "a1"));

            sut.Merge(new[] { a }, null!).Should().BeSameAs(a);
        }

        [Fact]
        public void Merger_ConJoinFieldEnBlancoYVariosDataSets_LanzaArgumentExceptionConNombreDeParametro()
        {
            var sut = new DynamicDataSetMerger();
            var a = KeyedSet("A", ("1", "a1"));
            var b = KeyedSet("B", ("1", "b1"));

            Action act = () => sut.Merge(new[] { a, b }, "   ");

            act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("joinField");
        }

        // ---------- DetailsHistoryShipmentsDomainService ----------

        [Fact]
        public void DetailsHistory_ConDataSetVacio_RetornaListaVacia()
        {
            var sut = new DetailsHistoryShipmentsDomainService();

            sut.GetDetailsHistoryShipments(DataSet(Array.Empty<String>()), "HBL-1").DetailsHistoryShipments.Should().BeEmpty();
        }

        [Fact]
        public void DetailsHistory_ConIdLogNoNumerico_LoOrdenaComoCero()
        {
            var sut = new DetailsHistoryShipmentsDomainService();
            var dataSet = DataSet(Array.Empty<String>(),
                Row((ExternalDataFields.IdLog, "5"), (ExternalDataFields.MessageLog, "cinco")),
                Row((ExternalDataFields.IdLog, "abc"), (ExternalDataFields.MessageLog, "invalido")),
                Row((ExternalDataFields.IdLog, "2"), (ExternalDataFields.MessageLog, "dos")));

            sut.GetDetailsHistoryShipments(dataSet, "HBL-1").DetailsHistoryShipments.Select(x => x.Message).Should().Equal("invalido", "dos", "cinco");
        }

        [Fact]
        public void DetailsHistory_ConFechaInvalida_RetornaFechaMinima()
        {
            var sut = new DetailsHistoryShipmentsDomainService();
            var dataSet = DataSet(Array.Empty<String>(), Row((ExternalDataFields.IdLog, "1"), (ExternalDataFields.ChangeDateLog, "no-fecha")));

            sut.GetDetailsHistoryShipments(dataSet, "HBL-1").DetailsHistoryShipments.Single().ChangeDate.Should().Be(DateTime.MinValue);
        }

        [Fact]
        public void DetailsHistory_MapeaFechaYUsuarioYEstados()
        {
            var sut = new DetailsHistoryShipmentsDomainService();
            var dataSet = DataSet(Array.Empty<String>(), Row(
                (ExternalDataFields.IdLog, "1"),
                (ExternalDataFields.ChangeDateLog, "2025-05-06 07:08:09"),
                (ExternalDataFields.ChangeUserLog, "jperez"),
                (ExternalDataFields.MessageLog, "msg"),
                (ExternalDataFields.OldStateLog, "Pendiente"),
                (ExternalDataFields.NewStateLog, "Entregado")));

            DetailsHistoryShipmentsDomainDtoResult item = sut.GetDetailsHistoryShipments(dataSet, "HBL-1").DetailsHistoryShipments.Single();

            item.ChangeDate.Should().Be(new DateTime(2025, 5, 6, 7, 8, 9));
            item.ChangeUser.Should().Be("jperez");
            item.Message.Should().Be("msg");
            item.OldState.Should().Be("Pendiente");
            item.NewState.Should().Be("Entregado");
        }

        [Fact]
        public void DetailsHistory_ConIdsIguales_ConservaElOrdenOriginal()
        {
            var sut = new DetailsHistoryShipmentsDomainService();
            var dataSet = DataSet(Array.Empty<String>(),
                Row((ExternalDataFields.IdLog, "1"), (ExternalDataFields.MessageLog, "a")),
                Row((ExternalDataFields.IdLog, "1"), (ExternalDataFields.MessageLog, "b")));

            sut.GetDetailsHistoryShipments(dataSet, "HBL-1").DetailsHistoryShipments.Select(x => x.Message).Should().Equal("a", "b");
        }
    }
}
