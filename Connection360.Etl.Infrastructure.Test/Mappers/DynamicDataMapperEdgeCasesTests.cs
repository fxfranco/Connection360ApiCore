using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Infrastructure.ExternalApi.DTOs;
using Connection360.Etl.Infrastructure.Mappers;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace Connection360.Etl.Infrastructure.Tests.Mappers
{
    public class DynamicDataMapperEdgeCasesTests
    {
        private static JsonElement Element(String json) => JsonSerializer.Deserialize<JsonElement>(json);

        private static DynamicRecord MapSingle(String field, Object? value)
        {
            var dto = new ExternalApiResponseDto
            {
                RequestedFields = new List<String> { field },
                Rows = new List<Dictionary<String, Object?>> { new() { [field] = value } }
            };
            return dto.ToDomainDataSet().Rows.Single();
        }

        [Theory]
        [InlineData("123", "123")]
        [InlineData("12.5", "12.5")]
        [InlineData("true", "True")]
        [InlineData("false", "False")]
        [InlineData("\"texto\"", "texto")]
        [InlineData("\"\"", "")]
        public void ToDomainDataSet_JsonElementPrimitivo_SeConvierteConToString(String json, String expected)
        {
            MapSingle("C", Element(json))["C"].Should().Be(expected);
        }

        [Fact]
        public void ToDomainDataSet_JsonElementObjetoOArreglo_SeConvierteASuTextoJson()
        {
            MapSingle("C", Element("{\"a\":1}"))["C"].Should().Be("{\"a\":1}");
            MapSingle("C", Element("[1,2]"))["C"].Should().Be("[1,2]");
        }

        [Fact]
        public void ToDomainDataSet_ValorNuloEnLaFila_MapeaCadenaVacia()
        {
            MapSingle("C", null)["C"].Should().BeEmpty();
        }

        [Fact]
        public void ToDomainDataSet_ValorNoJsonElementDeTexto_SeConservaTalCual()
        {
            MapSingle("C", "directo")["C"].Should().Be("directo");
        }

        [Fact]
        public void ToDomainDataSet_CamposQueNoEstanEnRequestedFields_SeIgnoran()
        {
            var dto = new ExternalApiResponseDto
            {
                RequestedFields = new List<String> { "A" },
                Rows = new List<Dictionary<String, Object?>> { new() { ["A"] = Element("\"1\""), ["EXTRA"] = Element("\"2\"") } }
            };

            DynamicRecord row = dto.ToDomainDataSet().Rows.Single();

            row.HasField("A").Should().BeTrue();
            row.HasField("EXTRA").Should().BeFalse();
        }

        [Fact]
        public void ToDomainDataSet_NombresDeCampoDelRegistro_SonInsensiblesAMayusculas()
        {
            DynamicRecord row = MapSingle("Campo", Element("\"v\""));

            row["CAMPO"].Should().Be("v");
            row["campo"].Should().Be("v");
        }

        [Fact]
        public void ToDomainDataSet_FilaSinNingunCampoSolicitado_ProduceUnRegistroSinCampos()
        {
            var dto = new ExternalApiResponseDto
            {
                RequestedFields = new List<String>(),
                Rows = new List<Dictionary<String, Object?>> { new() { ["A"] = Element("\"1\"") } }
            };

            DynamicDataSet result = dto.ToDomainDataSet();

            result.Rows.Should().ContainSingle();
            result.Rows[0].Fields.Should().BeEmpty();
            result.AvailableFields.Should().BeEmpty();
        }

        [Fact]
        public void ToDomainDataSet_CamposDuplicadosEnRequestedFields_NoLanza()
        {
            var dto = new ExternalApiResponseDto
            {
                RequestedFields = new List<String> { "A", "A" },
                Rows = new List<Dictionary<String, Object?>> { new() { ["A"] = Element("\"1\"") } }
            };

            Action act = () => dto.ToDomainDataSet();

            act.Should().NotThrow();
        }

        [Fact]
        public void ToDomainDataSet_PreservaElOrdenDeLasFilas()
        {
            var dto = new ExternalApiResponseDto
            {
                RequestedFields = new List<String> { "ID" },
                Rows = new List<Dictionary<String, Object?>>
                {
                    new() { ["ID"] = Element("\"3\"") },
                    new() { ["ID"] = Element("\"1\"") },
                    new() { ["ID"] = Element("\"2\"") },
                }
            };

            dto.ToDomainDataSet().Rows.Select(r => r["ID"]).Should().Equal("3", "1", "2");
        }
    }
}
