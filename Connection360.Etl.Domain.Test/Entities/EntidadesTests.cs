using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Connection360.Etl.Domain.Test.Entities
{
    public class ApplicationDataSheetTests
    {
        [Fact]
        public void Constructor_Defecto_TextosVaciosNumerosEnCeroYOpcionalesNulos()
        {
            var s = new ApplicationDataSheet();

            s.Id.Should().Be(0);
            s.FechaCreacion.Should().Be(default);
            foreach (var texto in new[]
            {
                s.TipoOperacion, s.Modalidad, s.Incoterm, s.Proveedor, s.Cliente, s.NitCliente, s.Origen,
                s.Destino, s.DescripcionMercancia, s.Estado, s.TipoCarga, s.Transportista, s.TipoDocumento,
                s.NombreDocumento, s.DocumentoTransporteHbl, s.FacturaProveedor, s.FacturaTcc, s.NumeroFactura,
                s.DescripcionGasto, s.Comentario
            })
            {
                texto.Should().Be(String.Empty);
            }
            s.TipoContenedor.Should().BeNull();
            s.NumeroContenedor.Should().BeNull();
            s.FechaDevolucionRealContenedor.Should().BeNull();
            s.CantidadContenedores.Should().Be(0);
            s.CantidadBultos.Should().Be(0);
            s.PesoKg.Should().Be(0m);
            s.VolumenM3.Should().Be(0m);
            s.DiasLibres.Should().Be(0);
            s.ValorTotalDemora.Should().Be(0m);
            s.TotalFacturaUsd.Should().Be(0m);
        }

        [Fact]
        public void Propiedades_AsignarValores_LosConservan()
        {
            var fecha = new DateTime(2025, 3, 4, 5, 6, 7);
            var s = new ApplicationDataSheet
            {
                Id = 10,
                FechaCreacion = fecha,
                TipoOperacion = "IMPO",
                Modalidad = "SEA",
                Incoterm = "FOB",
                Proveedor = "P",
                Cliente = "C",
                NitCliente = "900",
                Origen = "O",
                Destino = "D",
                DescripcionMercancia = "M",
                Estado = "E",
                TipoCarga = "FCL",
                TipoContenedor = "40HC",
                CantidadContenedores = 2,
                NumeroContenedor = "ABCD123",
                CantidadBultos = 3,
                PesoKg = 1.5m,
                VolumenM3 = 2.5m,
                Transportista = "T",
                TipoDocumento = "TD",
                NombreDocumento = "ND",
                DocumentoTransporteHbl = "HBL1",
                FechaBodegaOrigen = fecha,
                FechaEtd = fecha,
                FechaAtd = fecha,
                FechaEta = fecha,
                FechaAta = fecha,
                FechaBodegaDestino = fecha,
                FechaNacionalizacion = fecha,
                FechaDespachoDestino = fecha,
                FechaPlanilla = fecha,
                FechaEntregaContenedor = fecha,
                FechaDevolucionRealContenedor = fecha,
                DiasLibres = 1,
                DiasRestantesEntrega = 2,
                DiasDemoraContenedor = 3,
                ValorDiaDemora = 4m,
                ValorTotalDemora = 5m,
                DepositoContenedor = 6m,
                FechaSolicitudAnticipo = fecha,
                FechaPagoAnticipo = fecha,
                ValorAnticipo = 7m,
                FacturaProveedor = "FP",
                FacturaTcc = "FT",
                NumeroFactura = "NF",
                FechaFactura = fecha,
                DescripcionGasto = "DG",
                ValorGastoUsd = 8m,
                SubtotalFacturaUsd = 9m,
                IvaUsd = 10m,
                TotalFacturaUsd = 11m,
                Comentario = "COM",
                FechaComentario = fecha
            };

            s.Id.Should().Be(10);
            s.TipoContenedor.Should().Be("40HC");
            s.NumeroContenedor.Should().Be("ABCD123");
            s.FechaDevolucionRealContenedor.Should().Be(fecha);
            s.PesoKg.Should().Be(1.5m);
            s.TotalFacturaUsd.Should().Be(11m);
            s.FechaComentario.Should().Be(fecha);
            s.DocumentoTransporteHbl.Should().Be("HBL1");
        }
    }

    public class ApplicationDataSheetChangeTests
    {
        [Fact]
        public void Constructor_Defecto_ValoresIniciales()
        {
            var c = new ApplicationDataSheetChange();

            c.DocumentoTransporteHbl.Should().BeEmpty();
            c.NitCliente.Should().BeEmpty();
            c.IdOperacion.Should().Be(0);
            c.IsNewDocument.Should().BeFalse();
            c.StateChanged.Should().BeFalse();
            c.EstadoAnterior.Should().BeEmpty();
            c.NuevoEstado.Should().BeEmpty();
            c.CommentChanged.Should().BeFalse();
        }

        [Fact]
        public void Propiedades_AsignarValores_LosConservan()
        {
            var c = new ApplicationDataSheetChange
            {
                DocumentoTransporteHbl = "H",
                NitCliente = "N",
                IdOperacion = 5,
                IsNewDocument = true,
                StateChanged = true,
                EstadoAnterior = "A",
                NuevoEstado = "B",
                CommentChanged = true
            };

            c.DocumentoTransporteHbl.Should().Be("H");
            c.NitCliente.Should().Be("N");
            c.IdOperacion.Should().Be(5);
            c.IsNewDocument.Should().BeTrue();
            c.StateChanged.Should().BeTrue();
            c.EstadoAnterior.Should().Be("A");
            c.NuevoEstado.Should().Be("B");
            c.CommentChanged.Should().BeTrue();
        }
    }

    public class ApplicationDataSheetChangeSnapshotTests
    {
        [Fact]
        public void Constructor_Defecto_ValoresIniciales()
        {
            var s = new ApplicationDataSheetChangeSnapshot();

            s.Id.Should().Be(0);
            s.DocumentoTransporteHbl.Should().BeEmpty();
            s.Estado.Should().BeEmpty();
            s.Comentario.Should().BeEmpty();
            s.FechaComentario.Should().Be(default);
            s.FechaComentario.Should().Be(DateTime.MinValue);
        }

        [Fact]
        public void Propiedades_AsignarValores_LosConservan()
        {
            var f = new DateTime(2024, 1, 2);
            var s = new ApplicationDataSheetChangeSnapshot
            {
                Id = 3,
                DocumentoTransporteHbl = "H",
                Estado = "E",
                Comentario = "C",
                FechaComentario = f
            };

            s.Id.Should().Be(3);
            s.DocumentoTransporteHbl.Should().Be("H");
            s.Estado.Should().Be("E");
            s.Comentario.Should().Be("C");
            s.FechaComentario.Should().Be(f);
        }
    }

    public class LogStatusTrackingTests
    {
        [Fact]
        public void Constructor_Defecto_ValoresIniciales()
        {
            var l = new LogStatusTracking();

            l.IdOperacion.Should().Be(0);
            l.DocumentoTransporteHbl.Should().BeEmpty();
            l.FechaCambio.Should().Be(default);
            l.UsuarioCambio.Should().BeEmpty();
            l.Mensaje.Should().BeEmpty();
            l.EstadoAnterior.Should().BeEmpty();
            l.NuevoEstado.Should().BeEmpty();
        }

        [Fact]
        public void Propiedades_AsignarValores_LosConservan()
        {
            var f = new DateTime(2024, 1, 2);
            var l = new LogStatusTracking
            {
                IdOperacion = 9,
                DocumentoTransporteHbl = "H",
                FechaCambio = f,
                UsuarioCambio = "u",
                Mensaje = "m",
                EstadoAnterior = "a",
                NuevoEstado = "n"
            };

            l.IdOperacion.Should().Be(9);
            l.DocumentoTransporteHbl.Should().Be("H");
            l.FechaCambio.Should().Be(f);
            l.UsuarioCambio.Should().Be("u");
            l.Mensaje.Should().Be("m");
            l.EstadoAnterior.Should().Be("a");
            l.NuevoEstado.Should().Be("n");
        }
    }

    public class OutboxMessageTests
    {
        [Fact]
        public void Constructor_Defecto_ValoresIniciales()
        {
            var m = new OutboxMessage();

            m.Id.Should().Be(Guid.Empty);
            m.EventType.Should().BeEmpty();
            m.Payload.Should().BeEmpty();
            m.CreatedAt.Should().Be(default);
        }

        [Fact]
        public void Propiedades_AsignarValores_LosConservan()
        {
            var id = Guid.NewGuid();
            var f = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            var m = new OutboxMessage { Id = id, EventType = "ChangeState", Payload = "{}", CreatedAt = f };

            m.Id.Should().Be(id);
            m.EventType.Should().Be("ChangeState");
            m.Payload.Should().Be("{}");
            m.CreatedAt.Should().Be(f);
        }
    }

    public class EtlJobControlTests
    {
        [Fact]
        public void Constructor_Defecto_ValoresIniciales()
        {
            var j = new EtlJobControl();

            j.Id.Should().Be(0);
            j.JobName.Should().Be(EtlJobName.ApplicationDataSheet);
            j.Status.Should().Be(EtlJobStatus.Processing);
            j.LastProcessedPage.Should().BeNull();
            j.PageSize.Should().BeNull();
            j.TotalRecordsProcessed.Should().BeNull();
            j.UpdatedAt.Should().Be(default);
        }

        [Fact]
        public void Propiedades_AsignarValores_LosConservan()
        {
            var f = new DateTime(2024, 1, 2);
            var j = new EtlJobControl
            {
                Id = 1,
                JobName = EtlJobName.LogStatusTracking,
                Status = EtlJobStatus.Failed,
                LastProcessedPage = 4,
                PageSize = 100,
                TotalRecordsProcessed = 400,
                UpdatedAt = f
            };

            j.Id.Should().Be(1);
            j.JobName.Should().Be(EtlJobName.LogStatusTracking);
            j.Status.Should().Be(EtlJobStatus.Failed);
            j.LastProcessedPage.Should().Be(4);
            j.PageSize.Should().Be(100);
            j.TotalRecordsProcessed.Should().Be(400);
            j.UpdatedAt.Should().Be(f);
        }
    }
}
