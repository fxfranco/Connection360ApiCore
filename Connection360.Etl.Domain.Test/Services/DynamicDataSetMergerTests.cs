using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Domain.Enums;
using Connection360.Etl.Domain.Interfaces;
using Connection360.Etl.Domain.Services;
using FluentAssertions;
using Xunit;
using static Connection360.Etl.Domain.Test.TestSupport.Builders;

namespace Connection360.Etl.Domain.Test.Services
{
    public class DynamicDataSetMergerTests
    {
        private const String Key = "HBL";
        private readonly DynamicDataSetMerger _sut = new();

        [Fact]
        public void Implementa_IDynamicDataSetMerger()
        {
            _sut.Should().BeAssignableTo<IDynamicDataSetMerger>();
        }

        [Fact]
        public void Merge_ColeccionNula_RetornaDataSetVacio()
        {
            var result = _sut.Merge(null!, Key);

            result.AvailableFields.Should().BeEmpty();
            result.Rows.Should().BeEmpty();
        }

        [Fact]
        public void Merge_ColeccionVacia_RetornaDataSetVacioSinValidarJoinField()
        {
            var result = _sut.Merge(Array.Empty<DynamicDataSet>(), null!);

            result.AvailableFields.Should().BeEmpty();
            result.Rows.Should().BeEmpty();
        }

        [Fact]
        public void Merge_SoloElementosNulos_RetornaDataSetVacio()
        {
            var result = _sut.Merge(new DynamicDataSet[] { null!, null! }, Key);

            result.Rows.Should().BeEmpty();
        }

        [Fact]
        public void Merge_UnSoloDataSet_LoRetornaTalCual()
        {
            var ds = DataSet(new[] { Key, "A" }, Record((Key, "1"), ("A", "x")));

            var result = _sut.Merge(new[] { ds }, Key);

            result.Should().BeSameAs(ds);
        }

        [Fact]
        public void Merge_UnSoloDataSetMasNulos_LoRetornaTalCualSinValidarJoinField()
        {
            var ds = DataSet(new[] { Key }, Record((Key, "1")));

            var result = _sut.Merge(new DynamicDataSet[] { null!, ds }, "");

            result.Should().BeSameAs(ds);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Merge_VariosDataSetsYJoinFieldInvalido_LanzaArgumentException(String? joinField)
        {
            var a = DataSet(new[] { Key }, Record((Key, "1")));
            var b = DataSet(new[] { Key }, Record((Key, "2")));

            Action act = () => _sut.Merge(new[] { a, b }, joinField!);

            act.Should().Throw<ArgumentException>().WithParameterName("joinField");
        }

        [Fact]
        public void Merge_FullOuter_UneFilasConLlaveComunYConservaLasExclusivas()
        {
            var a = DataSet(new[] { Key, "A" },
                Record((Key, "1"), ("A", "a1")),
                Record((Key, "2"), ("A", "a2")));
            var b = DataSet(new[] { Key, "B" },
                Record((Key, "2"), ("B", "b2")),
                Record((Key, "3"), ("B", "b3")));

            var result = _sut.Merge(new[] { a, b }, Key);

            result.Rows.Should().HaveCount(3);
            result.Rows[0][Key].Should().Be("1");
            result.Rows[0]["A"].Should().Be("a1");
            result.Rows[0].HasField("B").Should().BeFalse();
            result.Rows[1][Key].Should().Be("2");
            result.Rows[1]["A"].Should().Be("a2");
            result.Rows[1]["B"].Should().Be("b2");
            result.Rows[2][Key].Should().Be("3");
            result.Rows[2]["B"].Should().Be("b3");
        }

        [Fact]
        public void Merge_PorDefecto_EsFullOuter()
        {
            var a = DataSet(new[] { Key }, Record((Key, "1")));
            var b = DataSet(new[] { Key }, Record((Key, "2")));

            _sut.Merge(new[] { a, b }, Key).Rows.Should().HaveCount(2);
        }

        [Fact]
        public void Merge_Inner_SoloConservaLlavesPresentesEnTodosLosDataSets()
        {
            var a = DataSet(new[] { Key, "A" },
                Record((Key, "1"), ("A", "a1")),
                Record((Key, "2"), ("A", "a2")));
            var b = DataSet(new[] { Key, "B" },
                Record((Key, "2"), ("B", "b2")),
                Record((Key, "3"), ("B", "b3")));

            var result = _sut.Merge(new[] { a, b }, Key, DataSetJoinType.Inner);

            result.Rows.Should().ContainSingle();
            result.Rows[0][Key].Should().Be("2");
            result.Rows[0]["A"].Should().Be("a2");
            result.Rows[0]["B"].Should().Be("b2");
        }

        [Fact]
        public void Merge_InnerConTresDataSets_RequiereLlaveEnLosTres()
        {
            var a = DataSet(new[] { Key }, Record((Key, "1")), Record((Key, "2")));
            var b = DataSet(new[] { Key }, Record((Key, "1")), Record((Key, "2")));
            var c = DataSet(new[] { Key }, Record((Key, "2")), Record((Key, "9")));

            var result = _sut.Merge(new[] { a, b, c }, Key, DataSetJoinType.Inner);

            result.Rows.Select(r => r[Key]).Should().Equal("2");
        }

        [Fact]
        public void Merge_InnerSinInterseccion_RetornaFilasVaciasPeroConColumnas()
        {
            var a = DataSet(new[] { Key, "A" }, Record((Key, "1")));
            var b = DataSet(new[] { Key, "B" }, Record((Key, "2")));

            var result = _sut.Merge(new[] { a, b }, Key, DataSetJoinType.Inner);

            result.Rows.Should().BeEmpty();
            result.AvailableFields.Should().Equal(Key, "A", "B");
        }

        [Fact]
        public void Merge_Columnas_UnionSinDuplicadosInsensibleAMayusculasYEnOrdenDeAparicion()
        {
            var a = DataSet(new[] { Key, "A", "Comun" }, Record((Key, "1")));
            var b = DataSet(new[] { "comun", "B", Key.ToLowerInvariant() }, Record((Key, "1")));

            var result = _sut.Merge(new[] { a, b }, Key);

            result.AvailableFields.Should().Equal(Key, "A", "Comun", "B");
        }

        [Fact]
        public void Merge_ValorExistenteNoSeSobrescribeConVacio()
        {
            var a = DataSet(new[] { Key, "V" }, Record((Key, "1"), ("V", "original")));
            var b = DataSet(new[] { Key, "V" }, Record((Key, "1"), ("V", "")));

            var result = _sut.Merge(new[] { a, b }, Key);

            result.Rows.Should().ContainSingle();
            result.Rows[0]["V"].Should().Be("original");
        }

        [Fact]
        public void Merge_ValorVacioSePuedeCompletarConValorPosterior()
        {
            var a = DataSet(new[] { Key, "V" }, Record((Key, "1"), ("V", "")));
            var b = DataSet(new[] { Key, "V" }, Record((Key, "1"), ("V", "lleno")));

            var result = _sut.Merge(new[] { a, b }, Key);

            result.Rows[0]["V"].Should().Be("lleno");
        }

        [Fact]
        public void Merge_ValorNoVacioPosterior_NoPisaElPrimeroNoVacio()
        {
            var a = DataSet(new[] { Key, "V" }, Record((Key, "1"), ("V", "primero")));
            var b = DataSet(new[] { Key, "V" }, Record((Key, "1"), ("V", "segundo")));

            var result = _sut.Merge(new[] { a, b }, Key);

            result.Rows[0]["V"].Should().Be("primero");
        }

        [Fact]
        public void Merge_LlavesComparadasSinImportarMayusculas()
        {
            var a = DataSet(new[] { Key, "A" }, Record((Key, "abc"), ("A", "1")));
            var b = DataSet(new[] { Key, "B" }, Record((Key, "ABC"), ("B", "2")));

            var result = _sut.Merge(new[] { a, b }, Key, DataSetJoinType.Inner);

            result.Rows.Should().ContainSingle();
            result.Rows[0]["A"].Should().Be("1");
            result.Rows[0]["B"].Should().Be("2");
        }

        [Fact]
        public void Merge_FilasSinLlaveOConLlaveVacia_SeDescartan()
        {
            var a = DataSet(new[] { Key, "A" },
                Record((Key, ""), ("A", "x")),
                Record(("A", "sin llave")),
                Record((Key, "1"), ("A", "ok")));
            var b = DataSet(new[] { Key }, Record((Key, "1")));

            var result = _sut.Merge(new[] { a, b }, Key);

            result.Rows.Should().ContainSingle();
            result.Rows[0]["A"].Should().Be("ok");
        }

        [Fact]
        public void Merge_FilasDuplicadasEnElMismoDataSet_SeFusionanEnUna()
        {
            var a = DataSet(new[] { Key, "A", "B" },
                Record((Key, "1"), ("A", "a")),
                Record((Key, "1"), ("B", "b")));
            var b = DataSet(new[] { Key }, Record((Key, "2")));

            var result = _sut.Merge(new[] { a, b }, Key);

            result.Rows.Should().HaveCount(2);
            result.Rows[0]["A"].Should().Be("a");
            result.Rows[0]["B"].Should().Be("b");
        }

        [Fact]
        public void Merge_ConNulosIntercalados_LosIgnoraYFusionaElResto()
        {
            var a = DataSet(new[] { Key, "A" }, Record((Key, "1"), ("A", "a")));
            var b = DataSet(new[] { Key, "B" }, Record((Key, "1"), ("B", "b")));

            var result = _sut.Merge(new DynamicDataSet?[] { a, null, b }!, Key);

            result.Rows.Should().ContainSingle();
            result.Rows[0]["A"].Should().Be("a");
            result.Rows[0]["B"].Should().Be("b");
        }

        [Fact]
        public void Merge_ConEnumerableDiferido_FuncionaConIEnumerable()
        {
            IEnumerable<DynamicDataSet> Sets()
            {
                yield return DataSet(new[] { Key }, Record((Key, "1")));
                yield return DataSet(new[] { Key }, Record((Key, "2")));
            }

            _sut.Merge(Sets(), Key).Rows.Should().HaveCount(2);
        }

        [Fact]
        public void Merge_OrdenDeFilas_SigueElOrdenDeAparicionDeLasLlaves()
        {
            var a = DataSet(new[] { Key }, Record((Key, "z")), Record((Key, "a")));
            var b = DataSet(new[] { Key }, Record((Key, "m")), Record((Key, "a")));

            var result = _sut.Merge(new[] { a, b }, Key);

            result.Rows.Select(r => r[Key]).Should().Equal("z", "a", "m");
        }
    }
}
