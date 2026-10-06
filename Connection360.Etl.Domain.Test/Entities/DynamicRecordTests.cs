using Connection360.Etl.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Connection360.Etl.Domain.Test.Entities
{
    public class DynamicRecordTests
    {
        [Fact]
        public void Constructor_ConDiccionarioNulo_CreaRegistroVacio()
        {
            var record = new DynamicRecord(null!);

            record.Fields.Should().BeEmpty();
            record.GetValue("X").Should().BeEmpty();
        }

        [Fact]
        public void GetValue_CampoExistente_RetornaValor()
        {
            var record = new DynamicRecord(new Dictionary<String, String> { ["A"] = "1" });

            record.GetValue("A").Should().Be("1");
        }

        [Fact]
        public void GetValue_CampoInexistente_RetornaCadenaVacia()
        {
            var record = new DynamicRecord(new Dictionary<String, String> { ["A"] = "1" });

            record.GetValue("B").Should().Be(String.Empty);
        }

        [Fact]
        public void GetValue_ComparadorOrdinalPorDefecto_EsSensibleAMayusculas()
        {
            var record = new DynamicRecord(new Dictionary<String, String> { ["A"] = "1" });

            record.GetValue("a").Should().BeEmpty();
        }

        [Fact]
        public void GetValue_DiccionarioInsensibleAMayusculas_RespetaElComparador()
        {
            var dict = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase) { ["Campo"] = "v" };
            var record = new DynamicRecord(dict);

            record.GetValue("CAMPO").Should().Be("v");
        }

        [Fact]
        public void GetValue_ValorVacioExistente_RetornaVacio()
        {
            var record = new DynamicRecord(new Dictionary<String, String> { ["A"] = "" });

            record.GetValue("A").Should().BeEmpty();
        }

        [Fact]
        public void Indexador_EquivaleAGetValue()
        {
            var record = new DynamicRecord(new Dictionary<String, String> { ["A"] = "1" });

            record["A"].Should().Be("1");
            record["Z"].Should().BeEmpty();
        }

        [Fact]
        public void HasField_CampoPresenteYAusente_RetornaSegunExistencia()
        {
            var record = new DynamicRecord(new Dictionary<String, String> { ["A"] = "" });

            record.HasField("A").Should().BeTrue();
            record.HasField("B").Should().BeFalse();
        }

        [Fact]
        public void Fields_ExponeLosCamposOriginales()
        {
            var record = new DynamicRecord(new Dictionary<String, String> { ["A"] = "1", ["B"] = "2" });

            record.Fields.Should().HaveCount(2);
            record.Fields["A"].Should().Be("1");
            record.Fields.Keys.Should().BeEquivalentTo(new[] { "A", "B" });
        }
    }
}
