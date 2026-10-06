using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Domain.Interfaces;
using Connection360.Etl.Domain.Services;
using FluentAssertions;
using Xunit;

namespace Connection360.Etl.Domain.Test.Services
{
    public class ApplicationDataSheetChangeDetectorTests
    {
        private static readonly DateTime Fecha = new(2024, 4, 7, 10, 0, 0);
        private readonly ApplicationDataSheetChangeDetector _sut = new();

        private static ApplicationDataSheet Sheet(String hbl, String estado = "Pendiente", String comentario = "", DateTime? fechaComentario = null, String nit = "900")
            => new()
            {
                DocumentoTransporteHbl = hbl,
                Estado = estado,
                Comentario = comentario,
                FechaComentario = fechaComentario ?? default,
                NitCliente = nit
            };

        private static ApplicationDataSheetChangeSnapshot Snap(String hbl, Int64 id = 1, String estado = "Pendiente", String comentario = "", DateTime? fechaComentario = null)
            => new()
            {
                Id = id,
                DocumentoTransporteHbl = hbl,
                Estado = estado,
                Comentario = comentario,
                FechaComentario = fechaComentario ?? default
            };

        private static IReadOnlyDictionary<String, ApplicationDataSheetChangeSnapshot> Snapshots(params ApplicationDataSheetChangeSnapshot[] snaps)
            => snaps.ToDictionary(s => s.DocumentoTransporteHbl, s => s);

        [Fact]
        public void Implementa_IApplicationDataSheetChangeDetector()
        {
            _sut.Should().BeAssignableTo<IApplicationDataSheetChangeDetector>();
        }

        [Fact]
        public void DetectChanges_HojasActualesNulas_LanzaArgumentNullException()
        {
            Action act = () => _sut.DetectChanges(null!, Snapshots());

            act.Should().Throw<ArgumentNullException>().WithParameterName("currentSheets");
        }

        [Fact]
        public void DetectChanges_SnapshotsNulos_LanzaArgumentNullException()
        {
            Action act = () => _sut.DetectChanges(new List<ApplicationDataSheet>(), null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("previousSnapshotsByDocument");
        }

        [Fact]
        public void DetectChanges_SinHojas_RetornaListaVacia()
        {
            _sut.DetectChanges(new List<ApplicationDataSheet>(), Snapshots(Snap("A"))).Should().BeEmpty();
        }

        [Fact]
        public void DetectChanges_SinCambios_RetornaListaVacia()
        {
            var sheets = new List<ApplicationDataSheet> { Sheet("A", "Entregado", "ok", Fecha) };
            var snaps = Snapshots(Snap("A", 5, "Entregado", "ok", Fecha));

            _sut.DetectChanges(sheets, snaps).Should().BeEmpty();
        }

        [Fact]
        public void DetectChanges_CambioDeEstado_MarcaStateChangedConEstadoAnteriorYNuevo()
        {
            var sheets = new List<ApplicationDataSheet> { Sheet("A", "En tránsito", nit: "123") };
            var snaps = Snapshots(Snap("A", 42, "Pendiente"));

            var changes = _sut.DetectChanges(sheets, snaps);

            var c = changes.Should().ContainSingle().Subject;
            c.DocumentoTransporteHbl.Should().Be("A");
            c.NitCliente.Should().Be("123");
            c.IdOperacion.Should().Be(42);
            c.IsNewDocument.Should().BeFalse();
            c.StateChanged.Should().BeTrue();
            c.EstadoAnterior.Should().Be("Pendiente");
            c.NuevoEstado.Should().Be("En tránsito");
            c.CommentChanged.Should().BeFalse();
        }

        [Fact]
        public void DetectChanges_CambioDeComentarioTexto_MarcaCommentChanged()
        {
            var sheets = new List<ApplicationDataSheet> { Sheet("A", "Pendiente", "nuevo", Fecha) };
            var snaps = Snapshots(Snap("A", 1, "Pendiente", "viejo", Fecha));

            var c = _sut.DetectChanges(sheets, snaps).Should().ContainSingle().Subject;

            c.CommentChanged.Should().BeTrue();
            c.StateChanged.Should().BeFalse();
            c.IsNewDocument.Should().BeFalse();
        }

        [Fact]
        public void DetectChanges_SoloCambiaFechaComentario_MarcaCommentChanged()
        {
            var sheets = new List<ApplicationDataSheet> { Sheet("A", "Pendiente", "igual", Fecha.AddDays(1)) };
            var snaps = Snapshots(Snap("A", 1, "Pendiente", "igual", Fecha));

            var c = _sut.DetectChanges(sheets, snaps).Should().ContainSingle().Subject;

            c.CommentChanged.Should().BeTrue();
            c.StateChanged.Should().BeFalse();
        }

        [Fact]
        public void DetectChanges_CambianEstadoYComentario_MarcaAmbos()
        {
            var sheets = new List<ApplicationDataSheet> { Sheet("A", "Entregado", "c2", Fecha) };
            var snaps = Snapshots(Snap("A", 1, "Pendiente", "c1", Fecha));

            var c = _sut.DetectChanges(sheets, snaps).Should().ContainSingle().Subject;

            c.StateChanged.Should().BeTrue();
            c.CommentChanged.Should().BeTrue();
        }

        [Fact]
        public void DetectChanges_ComparacionDeTexto_EsSensibleAMayusculas()
        {
            var sheets = new List<ApplicationDataSheet> { Sheet("A", "ENTREGADO") };
            var snaps = Snapshots(Snap("A", 1, "Entregado"));

            var c = _sut.DetectChanges(sheets, snaps).Should().ContainSingle().Subject;

            c.StateChanged.Should().BeTrue();
        }

        [Fact]
        public void DetectChanges_DocumentoNuevoConEstado_GeneraCambioConIdCeroYEstadoAnteriorVacio()
        {
            var sheets = new List<ApplicationDataSheet> { Sheet("NUEVO", "Pendiente", nit: "77") };

            var c = _sut.DetectChanges(sheets, Snapshots()).Should().ContainSingle().Subject;

            c.IsNewDocument.Should().BeTrue();
            c.IdOperacion.Should().Be(0);
            c.StateChanged.Should().BeTrue();
            c.EstadoAnterior.Should().BeEmpty();
            c.NuevoEstado.Should().Be("Pendiente");
            c.CommentChanged.Should().BeFalse();
            c.NitCliente.Should().Be("77");
        }

        [Fact]
        public void DetectChanges_DocumentoNuevoConComentario_MarcaCommentChanged()
        {
            var sheets = new List<ApplicationDataSheet> { Sheet("NUEVO", "", "hola", Fecha) };

            var c = _sut.DetectChanges(sheets, Snapshots()).Should().ContainSingle().Subject;

            c.IsNewDocument.Should().BeTrue();
            c.StateChanged.Should().BeFalse();
            c.CommentChanged.Should().BeTrue();
        }

        [Fact]
        public void DetectChanges_DocumentoNuevoSoloConFechaComentario_MarcaCommentChanged()
        {
            var sheets = new List<ApplicationDataSheet> { Sheet("NUEVO", "", "", Fecha) };

            var c = _sut.DetectChanges(sheets, Snapshots()).Should().ContainSingle().Subject;

            c.CommentChanged.Should().BeTrue();
        }

        [Fact]
        public void DetectChanges_DocumentoNuevoCompletamenteVacio_NoGeneraCambio()
        {
            var sheets = new List<ApplicationDataSheet> { Sheet("NUEVO", "", "", null) };

            _sut.DetectChanges(sheets, Snapshots()).Should().BeEmpty();
        }

        [Fact]
        public void DetectChanges_MezclaDeDocumentos_ConservaOrdenYSoloIncluyeLosQueCambian()
        {
            var sheets = new List<ApplicationDataSheet>
            {
                Sheet("SIN_CAMBIO", "Pendiente"),
                Sheet("CAMBIA", "Entregado"),
                Sheet("NUEVO", "Pendiente"),
                Sheet("VACIO", "")
            };
            var snaps = Snapshots(Snap("SIN_CAMBIO", 1, "Pendiente"), Snap("CAMBIA", 2, "Pendiente"));

            var changes = _sut.DetectChanges(sheets, snaps);

            changes.Select(c => c.DocumentoTransporteHbl).Should().Equal("CAMBIA", "NUEVO");
            changes[0].IdOperacion.Should().Be(2);
            changes[1].IsNewDocument.Should().BeTrue();
        }

        [Fact]
        public void DetectChanges_LlaveDelDiccionarioConOtraCapitalizacion_SeConsideraDocumentoNuevo()
        {
            var sheets = new List<ApplicationDataSheet> { Sheet("abc", "Pendiente") };
            var snaps = new Dictionary<String, ApplicationDataSheetChangeSnapshot>
            {
                ["ABC"] = Snap("ABC", 9, "Pendiente")
            };

            var c = _sut.DetectChanges(sheets, snaps).Should().ContainSingle().Subject;

            c.IsNewDocument.Should().BeTrue();
        }

        [Fact]
        public void DetectChanges_DiccionarioConComparadorInsensible_EncuentraElSnapshot()
        {
            var sheets = new List<ApplicationDataSheet> { Sheet("abc", "Pendiente") };
            var snaps = new Dictionary<String, ApplicationDataSheetChangeSnapshot>(StringComparer.OrdinalIgnoreCase)
            {
                ["ABC"] = Snap("ABC", 9, "Pendiente")
            };

            _sut.DetectChanges(sheets, snaps).Should().BeEmpty();
        }

        [Fact]
        public void DetectChanges_ResultadoEsIndependienteEntreLlamadas()
        {
            var sheets = new List<ApplicationDataSheet> { Sheet("NUEVO", "X") };

            var first = _sut.DetectChanges(sheets, Snapshots());
            var second = _sut.DetectChanges(sheets, Snapshots());

            first.Should().NotBeSameAs(second);
            first.Should().HaveCount(1);
            second.Should().HaveCount(1);
        }
    }
}
