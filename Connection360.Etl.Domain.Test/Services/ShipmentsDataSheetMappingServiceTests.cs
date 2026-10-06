using Connection360.Etl.Domain.Constants;
using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Domain.Interfaces;
using Connection360.Etl.Domain.Services;
using FluentAssertions;
using Xunit;
using static Connection360.Etl.Domain.Test.TestSupport.Builders;
using F = Connection360.Etl.Domain.Constants.ExternalDataFields;

namespace Connection360.Etl.Domain.Test.Services
{
    public class ShipmentsDataSheetMappingServiceTests
    {
        private readonly ShipmentsDataSheetMappingService _sut = new();

        private static DynamicDataSet Ds(params DynamicRecord[] rows) => DataSet(Array.Empty<String>(), rows);

        private static DynamicRecord FullRecord() => Record(
            (F.CreationDate, "01/02/2024"),
            (F.OperationType, "IMPO"),
            (F.ShipmentMode, "SEA"),
            (F.Incoterm, "FOB"),
            (F.Supplier, "Proveedor SA"),
            (F.ClientName, "Cliente SAS"),
            (F.ClientNit, "900123456"),
            (F.Origin, "Shanghai"),
            (F.Destination, "Cartagena"),
            (F.MerchandiseDescription, "Repuestos"),
            (F.State, "En tránsito"),
            (F.LoadType, "FCL"),
            (F.ContainerType, "40HC"),
            (F.ContainerAmount, "2"),
            (F.ContainerNumber, "MSKU1234567"),
            (F.PackagesNumbers, "150"),
            (F.WeightKg, "1234.56"),
            (F.VolumeM3, "78.9"),
            (F.Carrier, "Maersk"),
            (F.DocumentType, "HBL"),
            (F.DocumentNumber, "HBL-001"),
            (F.StoreOriginDate, "02/02/2024"),
            (F.ETDDate, "03/02/2024"),
            (F.ATDDate, "04/02/2024"),
            (F.ETADate, "05/02/2024"),
            (F.ATADate, "06/02/2024"),
            (F.StoreDestinationDate, "07/02/2024"),
            (F.NationalizationDate, "08/02/2024"),
            (F.DispatchDestinationDate, "09/02/2024"),
            (F.FormDate, "10/02/2024"),
            (F.ContainerDeliveryDate, "11/02/2024"),
            (F.ActualContainerReturnDate, "12/02/2024"),
            (F.DaysOff, "7"),
            (F.DaysRemainingDelivery, "3"),
            (F.ContainerDelayDays, "4"),
            (F.CostDayOfDelay, "50.5"),
            (F.TotalCostContainerDelays, "202"),
            (F.ContainerDepot, "300"),
            (F.AdvancePaymentRequestDate, "13/02/2024"),
            (F.AdvancePaymentDate, "14/02/2024"),
            (F.AdvancePaymentAmount, "1000.25"),
            (F.SupplierInvoice, "FP-1"),
            (F.TCCInvoice, "FT-1"),
            (F.InvoiceNumber, "NF-1"),
            (F.InvoiceDate, "15/02/2024"),
            (F.ExpenseDescription, "Flete"),
            (F.ExpenseAmountUSD, "10.1"),
            (F.InvoiceSubtotalUSD, "20.2"),
            (F.IvaUSD, "3.8"),
            (F.TotalInvoiceUSD, "24"),
            (F.Comment, "Todo bien"),
            (F.CommentDate, "16/02/2024 08:09:10"));

        [Fact]
        public void Implementa_IShipmentsDataSheetMappingService()
        {
            _sut.Should().BeAssignableTo<IShipmentsDataSheetMappingService>();
        }

        [Fact]
        public void Map_DataSetNulo_LanzaArgumentNullException()
        {
            Action act = () => _sut.Map(null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("unifiedDataSet");
        }

        [Fact]
        public void Map_DataSetVacio_RetornaListaVacia()
        {
            _sut.Map(DynamicDataSet.Empty).Should().BeEmpty();
        }

        [Fact]
        public void Map_FilaCompleta_MapeaTodosLosCampos()
        {
            var result = _sut.Map(Ds(FullRecord()));

            var s = result.Should().ContainSingle().Subject;
            s.Id.Should().Be(0);
            s.FechaCreacion.Should().Be(new DateTime(2024, 2, 1));
            s.TipoOperacion.Should().Be("IMPO");
            s.Modalidad.Should().Be("SEA");
            s.Incoterm.Should().Be("FOB");
            s.Proveedor.Should().Be("Proveedor SA");
            s.Cliente.Should().Be("Cliente SAS");
            s.NitCliente.Should().Be("900123456");
            s.Origen.Should().Be("Shanghai");
            s.Destino.Should().Be("Cartagena");
            s.DescripcionMercancia.Should().Be("Repuestos");
            s.Estado.Should().Be("En tránsito");

            s.TipoCarga.Should().Be("FCL");
            s.TipoContenedor.Should().Be("40HC");
            s.CantidadContenedores.Should().Be(2);
            s.NumeroContenedor.Should().Be("MSKU1234567");
            s.CantidadBultos.Should().Be(150);
            s.PesoKg.Should().Be(1234.56m);
            s.VolumenM3.Should().Be(78.9m);

            s.Transportista.Should().Be("Maersk");
            s.TipoDocumento.Should().Be("HBL");
            s.NombreDocumento.Should().Be("HBL");
            s.DocumentoTransporteHbl.Should().Be("HBL-001");

            s.FechaBodegaOrigen.Should().Be(new DateTime(2024, 2, 2));
            s.FechaEtd.Should().Be(new DateTime(2024, 2, 3));
            s.FechaAtd.Should().Be(new DateTime(2024, 2, 4));
            s.FechaEta.Should().Be(new DateTime(2024, 2, 5));
            s.FechaAta.Should().Be(new DateTime(2024, 2, 6));
            s.FechaBodegaDestino.Should().Be(new DateTime(2024, 2, 7));
            s.FechaNacionalizacion.Should().Be(new DateTime(2024, 2, 8));
            s.FechaDespachoDestino.Should().Be(new DateTime(2024, 2, 9));
            s.FechaPlanilla.Should().Be(new DateTime(2024, 2, 10));
            s.FechaEntregaContenedor.Should().Be(new DateTime(2024, 2, 11));
            s.FechaDevolucionRealContenedor.Should().Be(new DateTime(2024, 2, 12));

            s.DiasLibres.Should().Be(7);
            s.DiasRestantesEntrega.Should().Be(3);
            s.DiasDemoraContenedor.Should().Be(4);
            s.ValorDiaDemora.Should().Be(50.5m);
            s.ValorTotalDemora.Should().Be(202m);
            s.DepositoContenedor.Should().Be(300m);

            s.FechaSolicitudAnticipo.Should().Be(new DateTime(2024, 2, 13));
            s.FechaPagoAnticipo.Should().Be(new DateTime(2024, 2, 14));
            s.ValorAnticipo.Should().Be(1000.25m);
            s.FacturaProveedor.Should().Be("FP-1");
            s.FacturaTcc.Should().Be("FT-1");
            s.NumeroFactura.Should().Be("NF-1");
            s.FechaFactura.Should().Be(new DateTime(2024, 2, 15));
            s.DescripcionGasto.Should().Be("Flete");
            s.ValorGastoUsd.Should().Be(10.1m);
            s.SubtotalFacturaUsd.Should().Be(20.2m);
            s.IvaUsd.Should().Be(3.8m);
            s.TotalFacturaUsd.Should().Be(24m);

            s.Comentario.Should().Be("Todo bien");
            s.FechaComentario.Should().Be(new DateTime(2024, 2, 16, 8, 9, 10));
        }

        [Fact]
        public void Map_SoloDocumento_UsaValoresPorDefectoYOpcionalesNulos()
        {
            var s = _sut.Map(Ds(Record((F.DocumentNumber, "HBL-2")))).Single();

            s.DocumentoTransporteHbl.Should().Be("HBL-2");
            s.FechaCreacion.Should().Be(DateTime.MinValue);
            s.FechaEtd.Should().Be(DateTime.MinValue);
            s.FechaComentario.Should().Be(DateTime.MinValue);
            s.TipoContenedor.Should().BeNull();
            s.NumeroContenedor.Should().BeNull();
            s.FechaDevolucionRealContenedor.Should().BeNull();
            s.CantidadContenedores.Should().Be(0);
            s.PesoKg.Should().Be(0m);
            s.TotalFacturaUsd.Should().Be(0m);
            s.Estado.Should().BeEmpty();
            s.Comentario.Should().BeEmpty();
            s.TipoDocumento.Should().BeEmpty();
            s.NombreDocumento.Should().BeEmpty();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Map_ContenedorConTextoVacioOBlanco_RetornaNull(String valor)
        {
            var s = _sut.Map(Ds(Record(
                (F.DocumentNumber, "H"),
                (F.ContainerType, valor),
                (F.ContainerNumber, valor)))).Single();

            s.TipoContenedor.Should().BeNull();
            s.NumeroContenedor.Should().BeNull();
        }

        [Fact]
        public void Map_ContenedorConTexto_ConservaElValorSinRecortar()
        {
            var s = _sut.Map(Ds(Record(
                (F.DocumentNumber, "H"),
                (F.ContainerType, " 20GP "),
                (F.ContainerNumber, "ABC")))).Single();

            s.TipoContenedor.Should().Be(" 20GP ");
            s.NumeroContenedor.Should().Be("ABC");
        }

        [Fact]
        public void Map_ValoresNumericosYFechasInvalidos_UsanDefectos()
        {
            var s = _sut.Map(Ds(Record(
                (F.DocumentNumber, "H"),
                (F.ContainerAmount, "x"),
                (F.PackagesNumbers, "y"),
                (F.WeightKg, "z"),
                (F.VolumeM3, "w"),
                (F.DaysOff, "?"),
                (F.ETDDate, "no es fecha"),
                (F.ActualContainerReturnDate, "tampoco")))).Single();

            s.CantidadContenedores.Should().Be(0);
            s.CantidadBultos.Should().Be(0);
            s.PesoKg.Should().Be(0m);
            s.VolumenM3.Should().Be(0m);
            s.DiasLibres.Should().Be(0);
            s.FechaEtd.Should().Be(DateTime.MinValue);
            s.FechaDevolucionRealContenedor.Should().BeNull();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Map_SinDocumentoOEnBlanco_DescartaLaFila(String doc)
        {
            var result = _sut.Map(Ds(
                Record((F.DocumentNumber, doc), (F.State, "X")),
                Record((F.State, "Y"))));

            result.Should().BeEmpty();
        }

        [Fact]
        public void Map_VariasFilas_ConservaOrdenYOmiteLasInvalidas()
        {
            var result = _sut.Map(Ds(
                Record((F.DocumentNumber, "A"), (F.State, "1")),
                Record((F.State, "sin doc")),
                Record((F.DocumentNumber, "B"), (F.State, "2")),
                Record((F.DocumentNumber, "A"), (F.State, "3"))));

            result.Select(r => r.DocumentoTransporteHbl).Should().Equal("A", "B", "A");
            result.Select(r => r.Estado).Should().Equal("1", "2", "3");
        }

        [Fact]
        public void Map_DocumentoConEspacios_ConservaElValorOriginal()
        {
            var s = _sut.Map(Ds(Record((F.DocumentNumber, " HBL ")))).Single();

            s.DocumentoTransporteHbl.Should().Be(" HBL ");
        }

        [Fact]
        public void Map_EsCompatibleConElDetectorDeCambios_ParaUnDocumentoNuevo()
        {
            var sheets = _sut.Map(Ds(FullRecord()));
            var changes = new ApplicationDataSheetChangeDetector()
                .DetectChanges(sheets, new Dictionary<String, ApplicationDataSheetChangeSnapshot>());

            var c = changes.Should().ContainSingle().Subject;
            c.IsNewDocument.Should().BeTrue();
            c.NuevoEstado.Should().Be("En tránsito");
            c.CommentChanged.Should().BeTrue();
            c.NitCliente.Should().Be("900123456");
        }
    }
}
