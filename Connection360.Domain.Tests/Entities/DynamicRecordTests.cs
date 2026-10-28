using Connection360.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Connection360.Domain.Tests.Entities
{
    public class DynamicRecordTests
    {
        [Fact]
        public void Constructor_ConDiccionarioNulo_NoLanzaExcepcionYUsaDiccionarioVacio()
        {
            var record = new DynamicRecord(null!);

            record.HasField("cualquiera").Should().BeFalse();
            record["cualquiera"].Should().Be(String.Empty);
        }

        [Fact]
        public void Indexador_ConCampoExistente_RetornaElValor()
        {
            var fields = new Dictionary<String, String> { ["NOMBRE"] = "Juan" };
            var record = new DynamicRecord(fields);

            record["NOMBRE"].Should().Be("Juan");
        }

        [Fact]
        public void Indexador_ConCampoInexistente_RetornaCadenaVacia()
        {
            var record = new DynamicRecord(new Dictionary<String, String>());

            record["NO_EXISTE"].Should().Be(String.Empty);
        }

        [Fact]
        public void GetValue_EsEquivalenteAlIndexador()
        {
            var fields = new Dictionary<String, String> { ["ID"] = "100" };
            var record = new DynamicRecord(fields);

            record.GetValue("ID").Should().Be(record["ID"]);
        }

        [Fact]
        public void HasField_ConCampoExistente_RetornaTrue()
        {
            var record = new DynamicRecord(new Dictionary<String, String> { ["ID"] = "1" });

            record.HasField("ID").Should().BeTrue();
        }

        [Fact]
        public void HasField_ConCampoInexistente_RetornaFalse()
        {
            var record = new DynamicRecord(new Dictionary<String, String>());

            record.HasField("ID").Should().BeFalse();
        }

        [Fact]
        public void Fields_ExponeElDiccionarioDeSoloLectura()
        {
            var fields = new Dictionary<String, String> { ["A"] = "1", ["B"] = "2" };
            var record = new DynamicRecord(fields);

            record.Fields.Should().HaveCount(2);
            record.Fields["A"].Should().Be("1");
        }

        [Fact]
        public void Constructor_ConDiccionarioPersonalizado_RespetaComparadorDeClavesProvisto()
        {
            // El diccionario fue creado con comparador case-sensitive por el llamador
            var fields = new Dictionary<String, String>(StringComparer.Ordinal) { ["nombre"] = "Juan" };
            var record = new DynamicRecord(fields);

            record["nombre"].Should().Be("Juan");
            record["NOMBRE"].Should().Be(String.Empty);
        }
    }
}
