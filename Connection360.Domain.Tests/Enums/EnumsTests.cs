using Connection360.Domain.Enum;
using Connection360.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Connection360.Domain.Tests.Enums
{
    public class EnumsTests
    {
        [Fact]
        public void ApplicationDataSheetViewScope_DefineLosTresAlcances()
        {
            System.Enum.GetNames<ApplicationDataSheetViewScope>().Should().Equal("Entregados", "NoEntregados", "Todos");
            ((Int32)ApplicationDataSheetViewScope.Entregados).Should().Be(0);
            ((Int32)ApplicationDataSheetViewScope.NoEntregados).Should().Be(1);
            ((Int32)ApplicationDataSheetViewScope.Todos).Should().Be(2);
        }

        [Fact]
        public void DataSetJoinType_DefineInnerYFullOuter()
        {
            System.Enum.GetNames<DataSetJoinType>().Should().Equal("Inner", "FullOuter");
            default(DataSetJoinType).Should().Be(DataSetJoinType.Inner);
        }

        [Fact]
        public void LogStatusTrackingViewField_DefineLasOchoColumnasEnOrden()
        {
            System.Enum.GetNames<LogStatusTrackingViewField>().Should().Equal(
                "Id", "IdOperacion", "DocumentoTransporteHbl", "FechaCambio", "UsuarioCambio", "Mensaje", "EstadoAnterior", "NuevoEstado");
        }

        [Fact]
        public void ApplicationDataSheetViewField_TieneValoresUnicosYConsecutivos()
        {
            ApplicationDataSheetViewField[] values = System.Enum.GetValues<ApplicationDataSheetViewField>();

            values.Should().OnlyHaveUniqueItems();
            values.Select(v => (Int32)v).Should().Equal(Enumerable.Range(0, values.Length));
            values.Length.Should().Be(54);
        }

        [Theory]
        [InlineData(ApplicationDataSheetViewField.Id, 0)]
        [InlineData(ApplicationDataSheetViewField.NitCliente, 7)]
        [InlineData(ApplicationDataSheetViewField.Estado, 11)]
        [InlineData(ApplicationDataSheetViewField.DocumentoTransporteHbl, 22)]
        [InlineData(ApplicationDataSheetViewField.FechaComentario, 53)]
        public void ApplicationDataSheetViewField_ConservaLasPosicionesDeColumnasClave(ApplicationDataSheetViewField field, Int32 expected)
        {
            ((Int32)field).Should().Be(expected);
        }

        [Fact]
        public void ApplicationDataSheetViewField_ContieneTodasLasColumnasDeLaVista()
        {
            String[] expected =
            {
                "Id", "FechaCreacion", "TipoOperacion", "Modalidad", "Incoterm", "Proveedor", "Cliente", "NitCliente", "Origen", "Destino",
                "DescripcionMercancia", "Estado", "TipoCarga", "TipoContenedor", "CantidadContenedores", "NumeroContenedor", "CantidadBultos",
                "PesoKg", "VolumenM3", "Transportista", "TipoDocumento", "NombreDocumento", "DocumentoTransporteHbl", "FechaBodegaOrigen",
                "FechaEtd", "FechaAtd", "FechaEta", "FechaAta", "FechaBodegaDestino", "FechaNacionalizacion", "FechaDespachoDestino",
                "FechaPlanilla", "FechaEntregaContenedor", "FechaDevolucionRealContenedor", "DiasLibres", "DiasRestantesEntrega",
                "DiasDemoraContenedor", "ValorDiaDemora", "ValorTotalDemora", "DepositoContenedor", "FechaSolicitudAnticipo", "FechaPagoAnticipo",
                "ValorAnticipo", "FacturaProveedor", "FacturaTcc", "NumeroFactura", "FechaFactura", "DescripcionGasto", "ValorGastoUsd",
                "SubtotalFacturaUsd", "IvaUsd", "TotalFacturaUsd", "Comentario", "FechaComentario"
            };

            System.Enum.GetNames<ApplicationDataSheetViewField>().Should().Equal(expected);
        }

        [Fact]
        public void UserRoleApplication_DefineLosCincoRolesYSeParseaPorNombre()
        {
            System.Enum.GetNames<UserRoleApplication>().Should().Equal("UNASSIGNED", "ADMIN", "CLIENT", "ANALISTAOPE", "ANALISTASAC");
            System.Enum.Parse<UserRoleApplication>("analistasac", ignoreCase: true).Should().Be(UserRoleApplication.ANALISTASAC);
            System.Enum.TryParse<UserRoleApplication>("SUPERUSER", out _).Should().BeFalse();
            default(UserRoleApplication).Should().Be(UserRoleApplication.UNASSIGNED);
        }
    }
}
