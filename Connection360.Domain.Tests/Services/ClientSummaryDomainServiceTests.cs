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
    public class ClientSummaryDomainServiceTests
    {
        private const String ClientId = "900123456";

        // Se usa el filtro real para validar el comportamiento de extremo a extremo del servicio de dominio.
        private readonly ClientSummaryDomainService _sut = new(new ClientRecordsFilterService());

        private static DynamicDataSet BuildSampleDataSet()
        {
            var rows = new[]
            {
                Row((ExternalDataFields.ClientNit, ClientId), (ExternalDataFields.OperationType, ExternalDataValues.Import),
                    (ExternalDataFields.ShipmentMode, ExternalDataValues.AirShipment), (ExternalDataFields.State, ExternalDataValues.WithIssuesState),
                    (ExternalDataFields.ID, "1"), (ExternalDataFields.DocumentNumber, "HBL-001"), (ExternalDataFields.Origin, "BOG"),
                    (ExternalDataFields.Destination, "MIA"), (ExternalDataFields.CreationDate, "01/01/2024")),

                Row((ExternalDataFields.ClientNit, ClientId), (ExternalDataFields.OperationType, ExternalDataValues.Export),
                    (ExternalDataFields.ShipmentMode, ExternalDataValues.OceanShipment), (ExternalDataFields.State, ExternalDataValues.InTransitState),
                    (ExternalDataFields.ID, "2"), (ExternalDataFields.DocumentNumber, "HBL-002"), (ExternalDataFields.Origin, "MIA"),
                    (ExternalDataFields.Destination, "BOG"), (ExternalDataFields.CreationDate, "02/01/2024")),

                // Registro de otro cliente: no debe contarse
                Row((ExternalDataFields.ClientNit, "OTRO-CLIENTE"), (ExternalDataFields.OperationType, ExternalDataValues.Import),
                    (ExternalDataFields.ID, "3"), (ExternalDataFields.DocumentNumber, "HBL-003")),
            };

            return DataSet(new[] { ExternalDataFields.ClientNit }, rows);
        }

        private static List<CustomersOfCollaboratorDtoResult> BuildCustomersOfCollaborator(params String[] customerNits)
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

        [Fact]
        public void Summarize_FiltraSoloLosRegistrosDelClienteSolicitado()
        {
            var dataSet = BuildSampleDataSet();

            ClientSummaryDomainResult result = _sut.Summarize(dataSet, ClientId, null, lastRecordsCount: 10);

            result.TotalClientRecords.Should().Be(2);
        }

        [Fact]
        public void Summarize_CuentaCorrectamenteImportsExportsAirOceanEIssues()
        {
            var dataSet = BuildSampleDataSet();

            ClientSummaryDomainResult result = _sut.Summarize(dataSet, ClientId, null, lastRecordsCount: 10);

            result.TotalImports.Should().Be(1);
            result.TotalExports.Should().Be(1);
            result.TotalAirShipments.Should().Be(1);
            result.TotalOceanShipments.Should().Be(1);
            result.TotalWithIssues.Should().Be(1);
        }

        [Fact]
        public void Summarize_RespetaElLimiteDeLastRecordsCount()
        {
            var dataSet = BuildSampleDataSet();

            ClientSummaryDomainResult result = _sut.Summarize(dataSet, ClientId, null, lastRecordsCount: 1);

            result.RecentShipments.Should().HaveCount(1);
        }

        [Fact]
        public void Summarize_OrdenaLosEnviosRecientesPorFechaDeCreacionDescendente()
        {
            var dataSet = BuildSampleDataSet();

            ClientSummaryDomainResult result = _sut.Summarize(dataSet, ClientId, null, lastRecordsCount: 10);

            result.RecentShipments.Select(x => x.DocumentNumber).Should().ContainInOrder("HBL-002", "HBL-001");
        }

        [Fact]
        public void Summarize_MapeaCorrectamenteLosCamposDeResumenCliente()
        {
            var dataSet = BuildSampleDataSet();

            ClientSummaryDomainResult result = _sut.Summarize(dataSet, ClientId, null, lastRecordsCount: 10);

            result.RecentShipments.Should().Contain(x =>
                x.Id == 1 &&
                x.DocumentNumber == "HBL-001" &&
                x.Origin == "BOG" &&
                x.Destination == "MIA" &&
                x.Status == ExternalDataValues.WithIssuesState &&
                x.OperationType == ExternalDataValues.Import &&
                x.ShipmentMode == ExternalDataValues.AirShipment);
        }

        [Fact]
        public void Summarize_ConIdNoNumerico_MapeaIdCero()
        {
            var dataSet = DataSet(new[] { ExternalDataFields.ClientNit },
                Row((ExternalDataFields.ClientNit, ClientId), (ExternalDataFields.ID, "no-numerico")));

            ClientSummaryDomainResult result = _sut.Summarize(dataSet, ClientId, null, lastRecordsCount: 10);

            result.RecentShipments.Should().ContainSingle(x => x.Id == 0);
        }

        [Fact]
        public void Summarize_SinRegistrosDelCliente_RetornaTotalesEnCero()
        {
            var dataSet = BuildSampleDataSet();

            ClientSummaryDomainResult result = _sut.Summarize(dataSet, "CLIENTE-INEXISTENTE", null, lastRecordsCount: 10);

            result.TotalClientRecords.Should().Be(0);
            result.RecentShipments.Should().BeEmpty();
        }

        [Fact]
        public void Summarize_ConClientesDeColaborador_IncluyeLosRegistrosDeTodosSusClientes()
        {
            var dataSet = BuildSampleDataSet();
            var customers = BuildCustomersOfCollaborator(ClientId, "OTRO-CLIENTE");

            ClientSummaryDomainResult result = _sut.Summarize(dataSet, "COLABORADOR-1", customers, lastRecordsCount: 10);

            result.TotalClientRecords.Should().Be(3);
        }

        [Fact]
        public void Summarize_DelegaElFiltradoAlClientRecordsFilterService()
        {
            var filterService = new Mock<IClientRecordsFilterService>();
            var dataSet = BuildSampleDataSet();
            var customers = BuildCustomersOfCollaborator(ClientId);

            filterService
                .Setup(f => f.Filter(dataSet, ClientId, customers))
                .Returns(new List<DynamicRecord> { dataSet.Rows[0] });

            var sut = new ClientSummaryDomainService(filterService.Object);

            ClientSummaryDomainResult result = sut.Summarize(dataSet, ClientId, customers, lastRecordsCount: 10);

            result.TotalClientRecords.Should().Be(1);
            filterService.Verify(f => f.Filter(dataSet, ClientId, customers), Times.Once);
        }

        [Fact]
        public void Filter_ConDocumentoYClienteCoincidentes_RetornaElRegistro()
        {
            var dataSet = BuildSampleDataSet();

            ResumenClienteDto result = _sut.Filter(dataSet, ClientId, null, "HBL-001");

            result.DocumentNumber.Should().Be("HBL-001");
            result.Origin.Should().Be("BOG");
        }

        [Fact]
        public void Filter_ConComparacionSinDistincionDeMayusculas_EncuentraElRegistro()
        {
            var dataSet = BuildSampleDataSet();

            ResumenClienteDto result = _sut.Filter(dataSet, ClientId, null, "hbl-001");

            result.DocumentNumber.Should().Be("HBL-001");
        }

        [Fact]
        public void Filter_SinCoincidencias_RetornaDtoVacio()
        {
            var dataSet = BuildSampleDataSet();

            ResumenClienteDto result = _sut.Filter(dataSet, ClientId, null, "NO-EXISTE");

            result.Should().NotBeNull();
            result.DocumentNumber.Should().Be(String.Empty);
            result.Id.Should().Be(0);
        }

        [Fact]
        public void Filter_ConClienteQueNoCoincide_RetornaDtoVacio()
        {
            var dataSet = BuildSampleDataSet();

            ResumenClienteDto result = _sut.Filter(dataSet, "CLIENTE-INEXISTENTE", null, "HBL-001");

            result.DocumentNumber.Should().Be(String.Empty);
        }

        [Fact]
        public void Filter_ConDocumentoDeOtroClienteDelColaborador_RetornaElRegistro()
        {
            var dataSet = BuildSampleDataSet();
            var customers = BuildCustomersOfCollaborator(ClientId, "OTRO-CLIENTE");

            ResumenClienteDto result = _sut.Filter(dataSet, "COLABORADOR-1", customers, "HBL-003");

            result.DocumentNumber.Should().Be("HBL-003");
            result.ClientNit.Should().Be("OTRO-CLIENTE");
        }
    }
}
