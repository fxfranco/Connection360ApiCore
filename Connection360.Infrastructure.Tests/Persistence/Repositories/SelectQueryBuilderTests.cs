using Connection360.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Xunit;

namespace Connection360.Infrastructure.Tests.Persistence.Repositories
{
    public class SelectQueryBuilderTests
    {
        private const String View = "connection360read.vw_log_status_tracking";

        [Fact]
        public void Build_SinFiltro_ArmaSelectConPuntoYComa()
        {
            SelectQueryBuilder.Build("id, mensaje", View)
                .Should().Be("SELECT id, mensaje FROM connection360read.vw_log_status_tracking;");
        }

        [Fact]
        public void Build_ConFiltro_AgregaWhereConMarcadorDeParametro()
        {
            SelectQueryBuilder.Build("id", View, "nit_cliente = @NitCliente")
                .Should().Be("SELECT id FROM connection360read.vw_log_status_tracking WHERE nit_cliente = @NitCliente;");
        }

        [Fact]
        public void Build_ConFiltroEnBlanco_LoTrataComoSinFiltro()
        {
            SelectQueryBuilder.Build("id", View, "  ")
                .Should().Be("SELECT id FROM connection360read.vw_log_status_tracking;");
        }

        [Fact]
        public void Build_ColumnasConCastATimestamp_SeAceptan()
        {
            SelectQueryBuilder.Build("id, fecha_creacion::timestamp", "vw_x")
                .Should().Be("SELECT id, fecha_creacion::timestamp FROM vw_x;");
        }

        [Theory]
        [InlineData("id; DROP TABLE x")]
        [InlineData("id--")]
        [InlineData("id) UNION SELECT 1")]
        [InlineData("'id'")]
        [InlineData("\"id\"")]
        public void Build_ColumnasConCaracteresNoPermitidos_Lanza(String columns)
        {
            Action act = () => SelectQueryBuilder.Build(columns, View);
            act.Should().Throw<ArgumentException>().WithParameterName("columns");
        }

        [Theory]
        [InlineData("vw; DROP TABLE x")]
        [InlineData("vw x")]
        [InlineData("vw,otra")]
        [InlineData("vw--")]
        public void Build_VistaConCaracteresNoPermitidos_Lanza(String view)
        {
            Action act = () => SelectQueryBuilder.Build("id", view);
            act.Should().Throw<ArgumentException>().WithParameterName("viewName");
        }

        [Theory]
        [InlineData("nit = '1' OR 1=1;")]
        [InlineData("nit = @N; DROP TABLE x")]
        [InlineData("nit = (SELECT 1)")]
        [InlineData("nit = 'x'")]
        public void Build_FiltroConCaracteresNoPermitidos_Lanza(String where)
        {
            Action act = () => SelectQueryBuilder.Build("id", View, where);
            act.Should().Throw<ArgumentException>().WithParameterName("whereClause");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Build_ColumnasOVistaVacias_Lanza(String? value)
        {
            Action sinColumnas = () => SelectQueryBuilder.Build(value!, View);
            Action sinVista = () => SelectQueryBuilder.Build("id", value!);

            sinColumnas.Should().Throw<ArgumentException>();
            sinVista.Should().Throw<ArgumentException>();
        }
    }
}
