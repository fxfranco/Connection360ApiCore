using Connection360.Etl.Domain.Entities;
using FluentAssertions;
using Xunit;
using static Connection360.Etl.Domain.Test.TestSupport.Builders;

namespace Connection360.Etl.Domain.Test.Entities
{
    public class DynamicDataSetTests
    {
        [Fact]
        public void Constructor_CopiaCamposYFilasEnOrden()
        {
            var r1 = Record(("A", "1"));
            var r2 = Record(("A", "2"));

            var ds = new DynamicDataSet(new[] { "A", "B" }, new[] { r1, r2 });

            ds.AvailableFields.Should().Equal("A", "B");
            ds.Rows.Should().HaveCount(2);
            ds.Rows[0].Should().BeSameAs(r1);
            ds.Rows[1].Should().BeSameAs(r2);
        }

        [Fact]
        public void Constructor_ModificarLaColeccionOriginal_NoAfectaAlDataSet()
        {
            var fields = new List<String> { "A" };
            var rows = new List<DynamicRecord> { Record(("A", "1")) };

            var ds = new DynamicDataSet(fields, rows);
            fields.Add("B");
            rows.Clear();

            ds.AvailableFields.Should().Equal("A");
            ds.Rows.Should().HaveCount(1);
        }

        [Fact]
        public void Constructor_ColeccionesVacias_CreaDataSetVacio()
        {
            var ds = new DynamicDataSet(Enumerable.Empty<String>(), Enumerable.Empty<DynamicRecord>());

            ds.AvailableFields.Should().BeEmpty();
            ds.Rows.Should().BeEmpty();
        }

        [Fact]
        public void Constructor_CamposNulos_LanzaArgumentNullException()
        {
            Action act = () => new DynamicDataSet(null!, Enumerable.Empty<DynamicRecord>());

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_FilasNulas_LanzaArgumentNullException()
        {
            Action act = () => new DynamicDataSet(Enumerable.Empty<String>(), null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Empty_NoTieneCamposNiFilas()
        {
            DynamicDataSet.Empty.AvailableFields.Should().BeEmpty();
            DynamicDataSet.Empty.Rows.Should().BeEmpty();
        }

        [Fact]
        public void Empty_RetornaSiempreLaMismaInstancia()
        {
            DynamicDataSet.Empty.Should().BeSameAs(DynamicDataSet.Empty);
        }
    }
}
