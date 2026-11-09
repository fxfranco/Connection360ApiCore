using System.Reflection;
using Connection360.Domain.Enums;
using Connection360.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Xunit;

namespace Connection360.Infrastructure.Tests.Persistence.Repositories
{
    /// <summary>
    /// <c>LogStatusTrackingViewColumns</c> y <c>ApplicationDataSheetViewColumns</c> son internas
    /// (sin InternalsVisibleTo), por lo que se ejercitan por reflexion.
    /// </summary>
    public class LogStatusTrackingViewColumnsTests
    {
        private static readonly Type ColumnsType = typeof(LogStatusTrackingViewRepository).Assembly
            .GetType("Connection360.Infrastructure.Persistence.Repositories.LogStatusTrackingViewColumns", throwOnError: true)!;

        private static String AllColumns() => (String)ColumnsType.GetField("AllColumnsSql", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;

        private static String Build(IEnumerable<LogStatusTrackingViewField>? fields)
        {
            MethodInfo method = ColumnsType.GetMethod("BuildSelectColumns", BindingFlags.Public | BindingFlags.Static)!;
            try
            {
                return (String)method.Invoke(null, new Object?[] { fields })!;
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException!;
            }
        }

        [Fact]
        public void AllColumnsSql_ContieTodasLasColumnasDeLaVistaEnOrden()
        {
            AllColumns().Should().Be("id, id_operacion, documento_transporte_hbl, fecha_cambio, usuario_cambio, mensaje, estado_anterior, nuevo_estado");
        }

        [Fact]
        public void BuildSelectColumns_ConTodosLosCampos_EquivaleAAllColumnsSql()
        {
            Build(Enum.GetValues<LogStatusTrackingViewField>()).Should().Be(AllColumns());
        }

        [Fact]
        public void BuildSelectColumns_ConUnSoloCampo_RetornaSoloEsaColumna()
        {
            Build(new[] { LogStatusTrackingViewField.Mensaje }).Should().Be("mensaje");
        }

        [Fact]
        public void BuildSelectColumns_ConCamposDesordenados_RespetaElOrdenFijoDeLaVista()
        {
            String result = Build(new[] { LogStatusTrackingViewField.NuevoEstado, LogStatusTrackingViewField.Id, LogStatusTrackingViewField.FechaCambio });

            result.Should().Be("id, fecha_cambio, nuevo_estado");
        }

        [Fact]
        public void BuildSelectColumns_ConCamposDuplicados_DescartaLosDuplicados()
        {
            String result = Build(new[] { LogStatusTrackingViewField.Id, LogStatusTrackingViewField.Id, LogStatusTrackingViewField.Mensaje, LogStatusTrackingViewField.Id });

            result.Should().Be("id, mensaje");
        }

        [Theory]
        [InlineData(LogStatusTrackingViewField.Id, "id")]
        [InlineData(LogStatusTrackingViewField.IdOperacion, "id_operacion")]
        [InlineData(LogStatusTrackingViewField.DocumentoTransporteHbl, "documento_transporte_hbl")]
        [InlineData(LogStatusTrackingViewField.FechaCambio, "fecha_cambio")]
        [InlineData(LogStatusTrackingViewField.UsuarioCambio, "usuario_cambio")]
        [InlineData(LogStatusTrackingViewField.Mensaje, "mensaje")]
        [InlineData(LogStatusTrackingViewField.EstadoAnterior, "estado_anterior")]
        [InlineData(LogStatusTrackingViewField.NuevoEstado, "nuevo_estado")]
        public void BuildSelectColumns_MapeaCadaCampoASuColumnaSql(LogStatusTrackingViewField field, String expected)
        {
            Build(new[] { field }).Should().Be(expected);
        }

        [Fact]
        public void BuildSelectColumns_SinCampos_LanzaArgumentException()
        {
            Action act = () => Build(new List<LogStatusTrackingViewField>());

            act.Should().Throw<ArgumentException>().WithMessage("*al menos un campo*");
        }

        [Fact]
        public void BuildSelectColumns_ConNull_LanzaArgumentException()
        {
            Action act = () => Build(null);

            act.Should().Throw<ArgumentException>();
        }

        // ---------- ApplicationDataSheetViewColumns (misma estructura) ----------

        private static readonly Type SheetColumnsType = typeof(LogStatusTrackingViewRepository).Assembly
            .GetType("Connection360.Infrastructure.Persistence.Repositories.ApplicationDataSheetViewColumns", throwOnError: true)!;

        private static String BuildSheet(IEnumerable<ApplicationDataSheetViewField>? fields)
        {
            MethodInfo method = SheetColumnsType.GetMethod("BuildSelectColumns", BindingFlags.Public | BindingFlags.Static)!;
            try
            {
                return (String)method.Invoke(null, new Object?[] { fields })!;
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException!;
            }
        }

        [Fact]
        public void ApplicationDataSheet_BuildSelectColumns_ConTodosLosCampos_EquivaleAAllColumnsSql()
        {
            String all = (String)SheetColumnsType.GetField("AllColumnsSql", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;

            BuildSheet(Enum.GetValues<ApplicationDataSheetViewField>()).Should().Be(all);
            all.Should().Contain("fecha_creacion::timestamp").And.StartWith("id, ");
        }

        [Fact]
        public void ApplicationDataSheet_BuildSelectColumns_RespetaOrdenYDescartaDuplicados()
        {
            String result = BuildSheet(new[] { ApplicationDataSheetViewField.Estado, ApplicationDataSheetViewField.Id, ApplicationDataSheetViewField.Estado });

            result.Should().Be("id, estado");
        }

        [Fact]
        public void ApplicationDataSheet_BuildSelectColumns_SinCamposONull_LanzaArgumentException()
        {
            Action vacio = () => BuildSheet(new List<ApplicationDataSheetViewField>());
            Action nulo = () => BuildSheet(null);

            vacio.Should().Throw<ArgumentException>();
            nulo.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ApplicationDataSheet_TodosLosValoresDelEnumTienenColumnaMapeada()
        {
            foreach (ApplicationDataSheetViewField field in Enum.GetValues<ApplicationDataSheetViewField>())
            {
                BuildSheet(new[] { field }).Should().NotBeNullOrWhiteSpace($"el campo {field} debe tener una columna SQL");
            }
        }
    }
}
