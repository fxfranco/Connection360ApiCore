using Connection360.Domain.Constans;
using Connection360.Domain.Dtos;
using Connection360.Domain.Entities;
using Connection360.Domain.Services;
using FluentAssertions;
using Xunit;
using static Connection360.Domain.Tests.TestSupport.DynamicDataSetBuilder;

namespace Connection360.Domain.Tests.Services
{
    public class ClientRecordsFilterServiceTests
    {
        private const String ClientId = "900123456";

        private readonly ClientRecordsFilterService _sut = new();

        private static DynamicDataSet BuildDataSet()
        {
            return DataSet(new[] { ExternalDataFields.ClientNit },
                Row((ExternalDataFields.ClientNit, ClientId), (ExternalDataFields.ID, "1")),
                Row((ExternalDataFields.ClientNit, "800111222"), (ExternalDataFields.ID, "2")),
                Row((ExternalDataFields.ClientNit, "NO-ASIGNADO"), (ExternalDataFields.ID, "3")));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Filter_ConClientIdNuloOVacio_RetornaTodosLosRegistros(String? clientId)
        {
            var dataSet = BuildDataSet();

            List<DynamicRecord> result = _sut.Filter(dataSet, clientId!, null);

            result.Should().HaveCount(3);
        }

        [Fact]
        public void Filter_SinClientesDeColaborador_FiltraPorElClientIdRecibido()
        {
            var dataSet = BuildDataSet();

            List<DynamicRecord> result = _sut.Filter(dataSet, ClientId, null);

            result.Should().ContainSingle();
            result[0][ExternalDataFields.ID].Should().Be("1");
        }

        [Fact]
        public void Filter_ConListaDeClientesVacia_FiltraPorElClientIdRecibido()
        {
            var dataSet = BuildDataSet();

            List<DynamicRecord> result = _sut.Filter(dataSet, ClientId, new List<CustomersOfCollaboratorDtoResult>());

            result.Should().ContainSingle();
            result[0][ExternalDataFields.ID].Should().Be("1");
        }

        [Fact]
        public void Filter_ConClientesDeColaborador_FiltraPorLaIdentificacionDeCadaCliente()
        {
            var dataSet = BuildDataSet();
            var customers = new List<CustomersOfCollaboratorDtoResult>
            {
                new() { IdCollaborator = 1, IdentificacionCollaborator = "COLABORADOR-1", IdCustomer = 10, IdentificacionCustomer = ClientId },
                new() { IdCollaborator = 1, IdentificacionCollaborator = "COLABORADOR-1", IdCustomer = 11, IdentificacionCustomer = "800111222" }
            };

            List<DynamicRecord> result = _sut.Filter(dataSet, "COLABORADOR-1", customers);

            result.Select(r => r[ExternalDataFields.ID]).Should().BeEquivalentTo(new[] { "1", "2" });
        }

        [Fact]
        public void Filter_ComparaLaIdentificacionSinDistincionDeMayusculas()
        {
            var dataSet = DataSet(new[] { ExternalDataFields.ClientNit },
                Row((ExternalDataFields.ClientNit, "ABC-123"), (ExternalDataFields.ID, "1")));

            List<DynamicRecord> result = _sut.Filter(dataSet, "abc-123", null);

            result.Should().ContainSingle();
        }

        [Fact]
        public void Filter_SinCoincidencias_RetornaListaVacia()
        {
            var dataSet = BuildDataSet();

            List<DynamicRecord> result = _sut.Filter(dataSet, "CLIENTE-INEXISTENTE", null);

            result.Should().BeEmpty();
        }
    }
}
