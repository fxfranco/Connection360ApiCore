using Connection360.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Connection360.Domain.Tests.Entities
{
    public class DynamicDataSetTests
    {
        [Fact]
        public void Constructor_ConCamposYFilas_ExponeListasDeSoloLectura()
        {
            var fields = new List<String> { "ID", "NOMBRE" };
            var rows = new List<DynamicRecord> { new(new Dictionary<String, String> { ["ID"] = "1" }) };

            var dataSet = new DynamicDataSet(fields, rows);

            dataSet.AvailableFields.Should().BeEquivalentTo(fields);
            dataSet.Rows.Should().HaveCount(1);
        }

        [Fact]
        public void Constructor_ConColeccionesVacias_NoLanzaExcepcion()
        {
            var dataSet = new DynamicDataSet(Enumerable.Empty<String>(), Enumerable.Empty<DynamicRecord>());

            dataSet.AvailableFields.Should().BeEmpty();
            dataSet.Rows.Should().BeEmpty();
        }

        [Fact]
        public void AvailableFields_EsUnaListaDeSoloLectura()
        {
            var dataSet = new DynamicDataSet(new[] { "A" }, Enumerable.Empty<DynamicRecord>());

            dataSet.AvailableFields.Should().BeAssignableTo<IReadOnlyList<String>>();
        }
    }
}
