using Connection360.Domain.Entities;
using Connection360.Domain.Enum;
using Connection360.Domain.Services;
using FluentAssertions;
using Xunit;
using static Connection360.Domain.Tests.TestSupport.DynamicDataSetBuilder;

namespace Connection360.Domain.Tests.Services
{
    public class DynamicDataSetMergerTests
    {
        private readonly DynamicDataSetMerger _sut = new();

        [Fact]
        public void Merge_ConColeccionNulaOVacia_RetornaDataSetVacio()
        {
            DynamicDataSet result = _sut.Merge(null!, "ID");

            result.Rows.Should().BeEmpty();
            result.AvailableFields.Should().BeEmpty();
        }

        [Fact]
        public void Merge_ConUnSoloDataSet_LoRetornaSinModificar()
        {
            var dataSet = DataSet(new[] { "ID" }, Row(("ID", "1")));

            DynamicDataSet result = _sut.Merge(new[] { dataSet }, "ID");

            result.Should().BeSameAs(dataSet);
        }

        [Fact]
        public void Merge_IgnoraDataSetsNulos()
        {
            var dataSet = DataSet(new[] { "ID" }, Row(("ID", "1")));

            DynamicDataSet result = _sut.Merge(new DynamicDataSet?[] { null, dataSet }!, "ID");

            result.Should().BeSameAs(dataSet);
        }

        [Fact]
        public void Merge_ConJoinFieldVacio_LanzaArgumentException()
        {
            var a = DataSet(new[] { "ID" }, Row(("ID", "1")));
            var b = DataSet(new[] { "ID" }, Row(("ID", "1")));

            Action act = () => _sut.Merge(new[] { a, b }, joinField: "");

            act.Should().Throw<ArgumentException>().WithParameterName("joinField");
        }

        [Fact]
        public void Merge_FullOuter_ConservaTodasLasLlavesDeAmbosDataSets()
        {
            var a = DataSet(new[] { "ID", "NOMBRE" }, Row(("ID", "1"), ("NOMBRE", "Juan")));
            var b = DataSet(new[] { "ID", "CIUDAD" }, Row(("ID", "2"), ("CIUDAD", "Bogota")));

            DynamicDataSet result = _sut.Merge(new[] { a, b }, "ID", DataSetJoinType.FullOuter);

            result.Rows.Should().HaveCount(2);
            result.AvailableFields.Should().Contain(new[] { "ID", "NOMBRE", "CIUDAD" });
        }

        [Fact]
        public void Merge_Inner_ConservaSoloLasLlavesPresentesEnTodosLosDataSets()
        {
            var a = DataSet(new[] { "ID", "NOMBRE" }, Row(("ID", "1"), ("NOMBRE", "Juan")), Row(("ID", "2"), ("NOMBRE", "Pedro")));
            var b = DataSet(new[] { "ID", "CIUDAD" }, Row(("ID", "2"), ("CIUDAD", "Bogota")));

            DynamicDataSet result = _sut.Merge(new[] { a, b }, "ID", DataSetJoinType.Inner);

            result.Rows.Should().HaveCount(1);
            result.Rows[0].GetValue("ID").Should().Be("2");
            result.Rows[0].GetValue("CIUDAD").Should().Be("Bogota");
        }

        [Fact]
        public void Merge_FusionaCamposDeLaMismaLlaveSinSobreescribirConVacios()
        {
            var a = DataSet(new[] { "ID", "NOMBRE" }, Row(("ID", "1"), ("NOMBRE", "Juan")));
            var b = DataSet(new[] { "ID", "NOMBRE" }, Row(("ID", "1"), ("NOMBRE", "")));

            DynamicDataSet result = _sut.Merge(new[] { a, b }, "ID", DataSetJoinType.FullOuter);

            result.Rows.Single().GetValue("NOMBRE").Should().Be("Juan");
        }

        [Fact]
        public void Merge_IgnoraFilasConLlaveVacia()
        {
            var a = DataSet(new[] { "ID" }, Row(("ID", "")));
            var b = DataSet(new[] { "ID" }, Row(("ID", "1")));

            DynamicDataSet result = _sut.Merge(new[] { a, b }, "ID", DataSetJoinType.FullOuter);

            result.Rows.Should().HaveCount(1);
        }

        [Fact]
        public void Merge_UnionDeCampos_NoDuplicaCamposRepetidosSinImportarMayusculas()
        {
            var a = DataSet(new[] { "id", "NOMBRE" }, Row(("id", "1"), ("NOMBRE", "Juan")));
            var b = DataSet(new[] { "ID", "CIUDAD" }, Row(("ID", "1"), ("CIUDAD", "Bogota")));

            DynamicDataSet result = _sut.Merge(new[] { a, b }, "ID", DataSetJoinType.FullOuter);

            result.AvailableFields.Count(f => f.Equals("ID", StringComparison.OrdinalIgnoreCase)).Should().Be(1);
        }
    }
}
