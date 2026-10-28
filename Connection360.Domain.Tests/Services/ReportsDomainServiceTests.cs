using Connection360.Domain.Constans;
using Connection360.Domain.Dtos;
using Connection360.Domain.Interfaces;
using Connection360.Domain.Services;
using FluentAssertions;
using Moq;
using Xunit;
using static Connection360.Domain.Tests.TestSupport.DynamicDataSetBuilder;

namespace Connection360.Domain.Tests.Services
{
    public class ReportsDomainServiceTests
    {
        private const String ClientId = "900123456";

        private readonly ReportsDomainService _sut = new(new ClientRecordsFilterService());

        [Fact]
        public void Summarize_CuentaCadaEstadoCorrectamente()
        {
            var dataSet = DataSet(new[] { ExternalDataFields.ClientNit },
                Row((ExternalDataFields.ClientNit, ClientId), (ExternalDataFields.State, ExternalDataValues.WithIssuesState)),
                Row((ExternalDataFields.ClientNit, ClientId), (ExternalDataFields.State, ExternalDataValues.DeliveredState)),
                Row((ExternalDataFields.ClientNit, ClientId), (ExternalDataFields.State, ExternalDataValues.DestinationCustomsState)),
                Row((ExternalDataFields.ClientNit, ClientId), (ExternalDataFields.State, ExternalDataValues.OriginCustomsState)),
                Row((ExternalDataFields.ClientNit, ClientId), (ExternalDataFields.State, ExternalDataValues.InTransitState)),
                Row((ExternalDataFields.ClientNit, ClientId), (ExternalDataFields.State, ExternalDataValues.PendingState)));

            ReportsSummaryDomainDtoResult result = _sut.Summarize(dataSet, ClientId, 5, null).Single();

            result.TotalClientRecords.Should().Be(6);
            result.TotalWithIssuesStatus.Should().Be(1);
            result.TotalDeliveredStatus.Should().Be(1);
            result.TotalDestinationCustomsStatus.Should().Be(1);
            result.TotalOriginCustomsStatus.Should().Be(1);
            result.TotalInTransitStatus.Should().Be(1);
            result.TotalPendingStatus.Should().Be(1);
        }

        [Fact]
        public void Summarize_CuentaImportsExportsAireYMar()
        {
            var dataSet = DataSet(new[] { ExternalDataFields.ClientNit },
                Row((ExternalDataFields.ClientNit, ClientId), (ExternalDataFields.OperationType, ExternalDataValues.Import), (ExternalDataFields.ShipmentMode, ExternalDataValues.AirShipment)),
                Row((ExternalDataFields.ClientNit, ClientId), (ExternalDataFields.OperationType, ExternalDataValues.Export), (ExternalDataFields.ShipmentMode, ExternalDataValues.OceanShipment)),
                Row((ExternalDataFields.ClientNit, ClientId), (ExternalDataFields.OperationType, ExternalDataValues.Export), (ExternalDataFields.ShipmentMode, ExternalDataValues.OceanShipment)));

            ReportsSummaryDomainDtoResult result = _sut.Summarize(dataSet, ClientId, 5, null).Single();

            result.TotalImports.Should().Be(1);
            result.TotalExports.Should().Be(2);
            result.TotalAirShipments.Should().Be(1);
            result.TotalOceanShipments.Should().Be(2);
        }

        [Fact]
        public void Summarize_SumaValoresMonetariosCorrectamente()
        {
            var dataSet = DataSet(new[] { ExternalDataFields.ClientNit },
                Row((ExternalDataFields.ClientNit, ClientId), (ExternalDataFields.TotalInvoiceUSD, "100.5"),
                    (ExternalDataFields.AdvancePaymentAmount, "50"), (ExternalDataFields.TotalCostContainerDelays, "20")),
                Row((ExternalDataFields.ClientNit, ClientId), (ExternalDataFields.TotalInvoiceUSD, "200"),
                    (ExternalDataFields.AdvancePaymentAmount, "30"), (ExternalDataFields.TotalCostContainerDelays, "5")));

            ReportsSummaryDomainDtoResult result = _sut.Summarize(dataSet, ClientId, 5, null).Single();

            result.TotalInvoiced.Should().Be(300.5);
            result.TotalAdvancePayment.Should().Be(80);
            result.TotalDelays.Should().Be(25);
        }

        [Fact]
        public void Summarize_AgrupaRutasFrecuentesYOrdenaDescendentemente()
        {
            var dataSet = DataSet(new[] { ExternalDataFields.ClientNit },
                Row((ExternalDataFields.ClientNit, ClientId), (ExternalDataFields.Origin, "BOG"), (ExternalDataFields.Destination, "MIA")),
                Row((ExternalDataFields.ClientNit, ClientId), (ExternalDataFields.Origin, "BOG"), (ExternalDataFields.Destination, "MIA")),
                Row((ExternalDataFields.ClientNit, ClientId), (ExternalDataFields.Origin, "MDE"), (ExternalDataFields.Destination, "JFK")));

            ReportsSummaryDomainDtoResult result = _sut.Summarize(dataSet, ClientId, 5, null).Single();

            result.FrequentRoutes.Should().NotBeNull();
            result.FrequentRoutes!.First().Origin.Should().Be("BOG");
            result.FrequentRoutes!.First().TotalRoute.Should().Be(2);
        }

        [Fact]
        public void Summarize_RespetaElLimiteDeFrequentRoutesCount()
        {
            var rows = Enumerable.Range(1, 10)
                .Select(i => Row((ExternalDataFields.ClientNit, ClientId), (ExternalDataFields.Origin, $"ORIGEN{i}"), (ExternalDataFields.Destination, "DEST")))
                .ToArray();
            var dataSet = DataSet(new[] { ExternalDataFields.ClientNit }, rows);

            ReportsSummaryDomainDtoResult result = _sut.Summarize(dataSet, ClientId, 3, null).Single();

            result.FrequentRoutes.Should().HaveCount(3);
        }

        [Fact]
        public void Summarize_FiltraPorClienteCorrectamente()
        {
            var dataSet = DataSet(new[] { ExternalDataFields.ClientNit },
                Row((ExternalDataFields.ClientNit, ClientId)),
                Row((ExternalDataFields.ClientNit, "OTRO-CLIENTE")));

            ReportsSummaryDomainDtoResult result = _sut.Summarize(dataSet, ClientId, 5, null).Single();

            result.ClientNit.Should().Be(ClientId);
            result.TotalClientRecords.Should().Be(1);
        }

        [Fact]
        public void Summarize_SinRegistros_RetornaListaVacia()
        {
            var dataSet = DataSet(new[] { ExternalDataFields.ClientNit });

            List<ReportsSummaryDomainDtoResult> results = _sut.Summarize(dataSet, ClientId, 5, null);

            results.Should().BeEmpty();
        }

        [Fact]
        public void Summarize_ConClientesDeColaborador_RetornaUnResumenPorCadaCliente()
        {
            var dataSet = DataSet(new[] { ExternalDataFields.ClientNit },
                Row((ExternalDataFields.ClientNit, ClientId), (ExternalDataFields.ClientName, "Cliente Uno")),
                Row((ExternalDataFields.ClientNit, ClientId), (ExternalDataFields.ClientName, "Cliente Uno")),
                Row((ExternalDataFields.ClientNit, "800111222"), (ExternalDataFields.ClientName, "Cliente Dos")),
                Row((ExternalDataFields.ClientNit, "NO-ASIGNADO"), (ExternalDataFields.ClientName, "Otro")));

            var customers = new List<CustomersOfCollaboratorDtoResult>
            {
                new() { IdCollaborator = 1, IdentificacionCollaborator = "COLABORADOR-1", IdCustomer = 10, IdentificacionCustomer = ClientId },
                new() { IdCollaborator = 1, IdentificacionCollaborator = "COLABORADOR-1", IdCustomer = 11, IdentificacionCustomer = "800111222" }
            };

            List<ReportsSummaryDomainDtoResult> results = _sut.Summarize(dataSet, "COLABORADOR-1", 5, customers);

            results.Should().HaveCount(2);
            results.Should().Contain(r => r.ClientNit == ClientId && r.ClientName == "Cliente Uno" && r.TotalClientRecords == 2);
            results.Should().Contain(r => r.ClientNit == "800111222" && r.ClientName == "Cliente Dos" && r.TotalClientRecords == 1);
        }

        [Fact]
        public void Summarize_DelegaElFiltradoAlClientRecordsFilterService()
        {
            var filterService = new Mock<IClientRecordsFilterService>();
            var dataSet = DataSet(new[] { ExternalDataFields.ClientNit });

            filterService
                .Setup(f => f.Filter(dataSet, ClientId, null))
                .Returns(new List<Connection360.Domain.Entities.DynamicRecord>());

            var sut = new ReportsDomainService(filterService.Object);

            List<ReportsSummaryDomainDtoResult> results = sut.Summarize(dataSet, ClientId, 5, null);

            results.Should().BeEmpty();
            filterService.Verify(f => f.Filter(dataSet, ClientId, null), Times.Once);
        }
    }
}
