using Connection360.Domain.Dtos;
using Connection360.Domain.Enums;
using Connection360.Domain.Interfaces;
using FluentAssertions;
using Xunit;

namespace Connection360.Domain.Tests.Dtos
{
    public class ViewAndOutboxDtosTests
    {
        [Fact]
        public void ApplicationDataSheetViewResultDto_ValoresPorDefecto()
        {
            var dto = new ApplicationDataSheetViewResultDto();

            dto.Id.Should().Be(0);
            dto.FechaCreacion.Should().Be(default);
            new[]
            {
                dto.TipoOperacion, dto.Modalidad, dto.Incoterm, dto.Proveedor, dto.Cliente, dto.NitCliente, dto.Origen, dto.Destino,
                dto.DescripcionMercancia, dto.Estado, dto.TipoCarga, dto.Transportista, dto.TipoDocumento, dto.NombreDocumento,
                dto.DocumentoTransporteHbl, dto.FacturaProveedor, dto.FacturaTcc, dto.NumeroFactura, dto.DescripcionGasto, dto.Comentario
            }.Should().OnlyContain(s => s == String.Empty);
            dto.TipoContenedor.Should().BeNull();
            dto.NumeroContenedor.Should().BeNull();
            dto.FechaDevolucionRealContenedor.Should().BeNull();
            dto.PesoKg.Should().Be(0m);
            dto.CantidadContenedores.Should().Be(0);
        }

        [Fact]
        public void ApplicationDataSheetViewResultDto_Propiedades_AsignanYRetornanElValorProvisto()
        {
            DateTime D(Int32 n) => new DateTime(2025, 1, n);
            var dto = new ApplicationDataSheetViewResultDto
            {
                Id = 1, FechaCreacion = D(1), TipoOperacion = "t", Modalidad = "m", Incoterm = "i", Proveedor = "p", Cliente = "c", NitCliente = "n",
                Origen = "o", Destino = "d", DescripcionMercancia = "dm", Estado = "e", TipoCarga = "tc", TipoContenedor = "tco",
                CantidadContenedores = 2, NumeroContenedor = "nc", CantidadBultos = 3, PesoKg = 4.5m, VolumenM3 = 5.5m, Transportista = "tr",
                TipoDocumento = "td", NombreDocumento = "nd", DocumentoTransporteHbl = "hbl", FechaBodegaOrigen = D(2), FechaEtd = D(3),
                FechaAtd = D(4), FechaEta = D(5), FechaAta = D(6), FechaBodegaDestino = D(7), FechaNacionalizacion = D(8),
                FechaDespachoDestino = D(9), FechaPlanilla = D(10), FechaEntregaContenedor = D(11), FechaDevolucionRealContenedor = D(12),
                DiasLibres = 6, DiasRestantesEntrega = 7, DiasDemoraContenedor = 8, ValorDiaDemora = 9m, ValorTotalDemora = 10m,
                DepositoContenedor = 11m, FechaSolicitudAnticipo = D(13), FechaPagoAnticipo = D(14), ValorAnticipo = 12m,
                FacturaProveedor = "fp", FacturaTcc = "ft", NumeroFactura = "nf", FechaFactura = D(15), DescripcionGasto = "dg",
                ValorGastoUsd = 13m, SubtotalFacturaUsd = 14m, IvaUsd = 15m, TotalFacturaUsd = 16m, Comentario = "co", FechaComentario = D(16)
            };

            dto.Id.Should().Be(1);
            dto.FechaCreacion.Should().Be(D(1));
            dto.TipoOperacion.Should().Be("t");
            dto.Modalidad.Should().Be("m");
            dto.Incoterm.Should().Be("i");
            dto.Proveedor.Should().Be("p");
            dto.Cliente.Should().Be("c");
            dto.NitCliente.Should().Be("n");
            dto.Origen.Should().Be("o");
            dto.Destino.Should().Be("d");
            dto.DescripcionMercancia.Should().Be("dm");
            dto.Estado.Should().Be("e");
            dto.TipoCarga.Should().Be("tc");
            dto.TipoContenedor.Should().Be("tco");
            dto.CantidadContenedores.Should().Be(2);
            dto.NumeroContenedor.Should().Be("nc");
            dto.CantidadBultos.Should().Be(3);
            dto.PesoKg.Should().Be(4.5m);
            dto.VolumenM3.Should().Be(5.5m);
            dto.Transportista.Should().Be("tr");
            dto.TipoDocumento.Should().Be("td");
            dto.NombreDocumento.Should().Be("nd");
            dto.DocumentoTransporteHbl.Should().Be("hbl");
            dto.FechaBodegaOrigen.Should().Be(D(2));
            dto.FechaEtd.Should().Be(D(3));
            dto.FechaAtd.Should().Be(D(4));
            dto.FechaEta.Should().Be(D(5));
            dto.FechaAta.Should().Be(D(6));
            dto.FechaBodegaDestino.Should().Be(D(7));
            dto.FechaNacionalizacion.Should().Be(D(8));
            dto.FechaDespachoDestino.Should().Be(D(9));
            dto.FechaPlanilla.Should().Be(D(10));
            dto.FechaEntregaContenedor.Should().Be(D(11));
            dto.FechaDevolucionRealContenedor.Should().Be(D(12));
            dto.DiasLibres.Should().Be(6);
            dto.DiasRestantesEntrega.Should().Be(7);
            dto.DiasDemoraContenedor.Should().Be(8);
            dto.ValorDiaDemora.Should().Be(9m);
            dto.ValorTotalDemora.Should().Be(10m);
            dto.DepositoContenedor.Should().Be(11m);
            dto.FechaSolicitudAnticipo.Should().Be(D(13));
            dto.FechaPagoAnticipo.Should().Be(D(14));
            dto.ValorAnticipo.Should().Be(12m);
            dto.FacturaProveedor.Should().Be("fp");
            dto.FacturaTcc.Should().Be("ft");
            dto.NumeroFactura.Should().Be("nf");
            dto.FechaFactura.Should().Be(D(15));
            dto.DescripcionGasto.Should().Be("dg");
            dto.ValorGastoUsd.Should().Be(13m);
            dto.SubtotalFacturaUsd.Should().Be(14m);
            dto.IvaUsd.Should().Be(15m);
            dto.TotalFacturaUsd.Should().Be(16m);
            dto.Comentario.Should().Be("co");
            dto.FechaComentario.Should().Be(D(16));
        }

        [Fact]
        public void LogStatusTrackingViewResultDto_ValoresPorDefectoYAsignacion()
        {
            var empty = new LogStatusTrackingViewResultDto();
            empty.Id.Should().Be(0);
            empty.IdOperacion.Should().Be(0);
            empty.FechaCambio.Should().Be(default);
            new[] { empty.DocumentoTransporteHbl, empty.UsuarioCambio, empty.Mensaje, empty.EstadoAnterior, empty.NuevoEstado }.Should().OnlyContain(s => s == String.Empty);

            var date = new DateTime(2025, 5, 6);
            var dto = new LogStatusTrackingViewResultDto
            {
                Id = 1, IdOperacion = 2, DocumentoTransporteHbl = "hbl", FechaCambio = date, UsuarioCambio = "u", Mensaje = "m", EstadoAnterior = "a", NuevoEstado = "n"
            };
            dto.Id.Should().Be(1);
            dto.IdOperacion.Should().Be(2);
            dto.DocumentoTransporteHbl.Should().Be("hbl");
            dto.FechaCambio.Should().Be(date);
            dto.UsuarioCambio.Should().Be("u");
            dto.Mensaje.Should().Be("m");
            dto.EstadoAnterior.Should().Be("a");
            dto.NuevoEstado.Should().Be("n");
        }

        [Fact]
        public void ApplicationDataSheetViewFieldsSelectionDto_IniciaConListaVaciaYPermiteAgregarCampos()
        {
            var dto = new ApplicationDataSheetViewFieldsSelectionDto();
            dto.Fields.Should().NotBeNull().And.BeEmpty();

            dto.Fields.Add(ApplicationDataSheetViewField.Id);
            dto.Fields.Should().Equal(ApplicationDataSheetViewField.Id);

            var list = new List<ApplicationDataSheetViewField> { ApplicationDataSheetViewField.Estado };
            dto.Fields = list;
            dto.Fields.Should().BeSameAs(list);
        }

        [Fact]
        public void LogStatusTrackingViewFieldsSelectionDto_IniciaConListaVaciaYPermiteAgregarCampos()
        {
            var dto = new LogStatusTrackingViewFieldsSelectionDto();
            dto.Fields.Should().NotBeNull().And.BeEmpty();

            dto.Fields.Add(LogStatusTrackingViewField.Mensaje);
            dto.Fields.Should().Equal(LogStatusTrackingViewField.Mensaje);

            var list = new List<LogStatusTrackingViewField> { LogStatusTrackingViewField.Id };
            dto.Fields = list;
            dto.Fields.Should().BeSameAs(list);
        }

        [Fact]
        public void OutboxMessagesRequestDto_ValoresPorDefectoYAsignacion()
        {
            var empty = new OutboxMessagesRequestDto();
            new[] { empty.ClientId, empty.EventType, empty.DocumentNumber, empty.Title, empty.Message }.Should().OnlyContain(s => s == String.Empty);
            empty.MessageDate.Should().BeNull();

            var date = new DateTime(2025, 1, 2);
            var dto = new OutboxMessagesRequestDto { ClientId = "c", EventType = "e", DocumentNumber = "d", Title = "t", Message = "m", MessageDate = date };
            dto.ClientId.Should().Be("c");
            dto.EventType.Should().Be("e");
            dto.DocumentNumber.Should().Be("d");
            dto.Title.Should().Be("t");
            dto.Message.Should().Be("m");
            dto.MessageDate.Should().Be(date);
        }

        [Fact]
        public void OutboxMessagesResultDto_ValoresPorDefectoYAsignacion()
        {
            var empty = new OutboxMessagesResultDto();
            empty.Id.Should().Be(Guid.Empty);
            empty.EventType.Should().BeEmpty();
            empty.Payload.Should().BeEmpty();

            Guid id = Guid.NewGuid();
            var dto = new OutboxMessagesResultDto { Id = id, EventType = "e", Payload = "{}" };
            dto.Id.Should().Be(id);
            dto.EventType.Should().Be("e");
            dto.Payload.Should().Be("{}");
        }

        [Fact]
        public void ResolveClientAccessRequest_AsignaTodasLasPropiedades()
        {
            var dto = new ResolveClientAccessRequest { IdClient = "1", RoleName = "ADMIN", FilterValue = "f", IdQueryClient = "2", AllClient = true };

            dto.IdClient.Should().Be("1");
            dto.RoleName.Should().Be("ADMIN");
            dto.FilterValue.Should().Be("f");
            dto.IdQueryClient.Should().Be("2");
            dto.AllClient.Should().BeTrue();
            new ResolveClientAccessRequest().AllClient.Should().BeFalse();
        }
    }
}
