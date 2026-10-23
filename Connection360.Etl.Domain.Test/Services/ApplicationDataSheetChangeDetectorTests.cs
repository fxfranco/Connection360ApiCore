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

        // ---------------------------------------------------------------- OtherInfo

        public static IEnumerable<Object[]> CamposDeOtraInformacion()
        {
            static Object[] Caso(String campo, Action<ApplicationDataSheet> cambio) => new Object[] { campo, cambio };

            yield return Caso("Transportista", s => s.Transportista = "TCC");
            yield return Caso("FechaSolicitudAnticipo", s => s.FechaSolicitudAnticipo = new DateTime(2024, 1, 1));
            yield return Caso("ValorAnticipo", s => s.ValorAnticipo = 150.25m);
            yield return Caso("FechaPagoAnticipo", s => s.FechaPagoAnticipo = new DateTime(2024, 1, 2));
            yield return Caso("FechaBodegaOrigen", s => s.FechaBodegaOrigen = new DateTime(2024, 1, 3));
            yield return Caso("FechaEtd", s => s.FechaEtd = new DateTime(2024, 1, 4));
            yield return Caso("FechaAtd", s => s.FechaAtd = new DateTime(2024, 1, 5));
            yield return Caso("FechaEta", s => s.FechaEta = new DateTime(2024, 1, 6));
            yield return Caso("FechaAta", s => s.FechaAta = new DateTime(2024, 1, 7));
            yield return Caso("FechaBodegaDestino", s => s.FechaBodegaDestino = new DateTime(2024, 1, 8));
            yield return Caso("FechaNacionalizacion", s => s.FechaNacionalizacion = new DateTime(2024, 1, 9));
            yield return Caso("FechaDespachoDestino", s => s.FechaDespachoDestino = new DateTime(2024, 1, 10));
            yield return Caso("FechaPlanilla", s => s.FechaPlanilla = new DateTime(2024, 1, 11));
            yield return Caso("FechaEntregaContenedor", s => s.FechaEntregaContenedor = new DateTime(2024, 1, 12));
            yield return Caso("DiasLibres", s => s.DiasLibres = 7);
            yield return Caso("DiasRestantesEntrega", s => s.DiasRestantesEntrega = 3);
            yield return Caso("FechaDevolucionRealContenedor", s => s.FechaDevolucionRealContenedor = new DateTime(2024, 1, 13));
            yield return Caso("DiasDemoraContenedor", s => s.DiasDemoraContenedor = 2);
            yield return Caso("ValorDiaDemora", s => s.ValorDiaDemora = 12.5m);
            yield return Caso("ValorTotalDemora", s => s.ValorTotalDemora = 99.5m);
        }

        [Theory]
        [MemberData(nameof(CamposDeOtraInformacion))]
        public void DetectChanges_CambiaUnCampoDeOtraInformacion_MarcaSoloOtherInfoChanged(String campo, Action<ApplicationDataSheet> cambio)
        {
            var sheet = Sheet("A", "Pendiente");
            cambio(sheet);
            var snaps = Snapshots(Snap("A", 8, "Pendiente"));

            var c = _sut.DetectChanges(new List<ApplicationDataSheet> { sheet }, snaps).Should().ContainSingle(because: campo).Subject;

            c.OtherInfoChanged.Should().BeTrue(campo);
            c.StateChanged.Should().BeFalse(campo);
            c.CommentChanged.Should().BeFalse(campo);
            c.IsNewDocument.Should().BeFalse(campo);
            c.IdOperacion.Should().Be(8);
        }

        [Fact]
        public void DetectChanges_CamposDeOtraInformacionSinCambios_NoGeneraCambio()
        {
            var sheet = Sheet("A", "Pendiente");
            sheet.Transportista = "TCC";
            sheet.ValorAnticipo = 10m;
            sheet.FechaEtd = new DateTime(2024, 2, 2);
            sheet.DiasLibres = 5;
            sheet.FechaDevolucionRealContenedor = new DateTime(2024, 2, 3);
            var snap = Snap("A", 1, "Pendiente");
            snap.Transportista = "TCC";
            snap.ValorAnticipo = 10m;
            snap.FechaEtd = new DateTime(2024, 2, 2);
            snap.DiasLibres = 5;
            snap.FechaDevolucionRealContenedor = new DateTime(2024, 2, 3);

            _sut.DetectChanges(new List<ApplicationDataSheet> { sheet }, Snapshots(snap)).Should().BeEmpty();
        }

        [Fact]
        public void DetectChanges_LasFechasSeComparanPorDia_LaHoraNoGeneraCambioFalso()
        {
            var sheet = Sheet("A", "Pendiente");
            sheet.FechaEta = new DateTime(2024, 3, 4, 15, 30, 0);
            sheet.FechaDevolucionRealContenedor = new DateTime(2024, 3, 5, 9, 0, 0);
            var snap = Snap("A", 1, "Pendiente");
            snap.FechaEta = new DateTime(2024, 3, 4); // la columna DATE no guarda la hora
            snap.FechaDevolucionRealContenedor = new DateTime(2024, 3, 5);

            _sut.DetectChanges(new List<ApplicationDataSheet> { sheet }, Snapshots(snap)).Should().BeEmpty();
        }

        [Fact]
        public void DetectChanges_FechaDevolucionDeNulaAFecha_MarcaOtherInfoChanged()
        {
            var sheet = Sheet("A", "Pendiente");
            sheet.FechaDevolucionRealContenedor = new DateTime(2024, 3, 5);

            var c = _sut.DetectChanges(new List<ApplicationDataSheet> { sheet }, Snapshots(Snap("A", 1, "Pendiente"))).Should().ContainSingle().Subject;

            c.OtherInfoChanged.Should().BeTrue();
        }

        [Fact]
        public void DetectChanges_FechaDevolucionDeFechaANula_MarcaOtherInfoChanged()
        {
            var snap = Snap("A", 1, "Pendiente");
            snap.FechaDevolucionRealContenedor = new DateTime(2024, 3, 5);

            var c = _sut.DetectChanges(new List<ApplicationDataSheet> { Sheet("A", "Pendiente") }, Snapshots(snap)).Should().ContainSingle().Subject;

            c.OtherInfoChanged.Should().BeTrue();
        }

        [Fact]
        public void DetectChanges_LosImportesSeComparanConDosDecimales_UnaDiferenciaMenorNoGeneraCambioFalso()
        {
            var sheet = Sheet("A", "Pendiente");
            sheet.ValorAnticipo = 10.004m;   // se guarda como 10.00
            sheet.ValorDiaDemora = 5.004m;    // se guarda como 5.00
            sheet.ValorTotalDemora = 20.005m; // se guarda como 20.01 (away from zero)
            var snap = Snap("A", 1, "Pendiente");
            snap.ValorAnticipo = 10.00m;
            snap.ValorDiaDemora = 5.00m;
            snap.ValorTotalDemora = 20.01m;

            _sut.DetectChanges(new List<ApplicationDataSheet> { sheet }, Snapshots(snap)).Should().BeEmpty();
        }

        [Fact]
        public void DetectChanges_UnImporteConDiferenciaDeUnCentavo_MarcaOtherInfoChanged()
        {
            var sheet = Sheet("A", "Pendiente");
            sheet.ValorAnticipo = 10.02m;
            var snap = Snap("A", 1, "Pendiente");
            snap.ValorAnticipo = 10.00m;

            var c = _sut.DetectChanges(new List<ApplicationDataSheet> { sheet }, Snapshots(snap)).Should().ContainSingle().Subject;

            c.OtherInfoChanged.Should().BeTrue();
        }

        [Theory]
        [InlineData("", null)]
        [InlineData(null, "")]
        public void DetectChanges_TransportistaNuloOVacio_SeTrataIgual(String? actual, String? anterior)
        {
            var sheet = Sheet("A", "Pendiente");
            sheet.Transportista = actual!;
            var snap = Snap("A", 1, "Pendiente");
            snap.Transportista = anterior!;

            _sut.DetectChanges(new List<ApplicationDataSheet> { sheet }, Snapshots(snap)).Should().BeEmpty();
        }

        [Fact]
        public void DetectChanges_TransportistaEsSensibleAMayusculas()
        {
            var sheet = Sheet("A", "Pendiente");
            sheet.Transportista = "tcc";
            var snap = Snap("A", 1, "Pendiente");
            snap.Transportista = "TCC";

            _sut.DetectChanges(new List<ApplicationDataSheet> { sheet }, Snapshots(snap)).Should().ContainSingle().Which.OtherInfoChanged.Should().BeTrue();
        }

        [Fact]
        public void DetectChanges_CambianEstadoComentarioYOtraInformacion_MarcaLosTres()
        {
            var sheet = Sheet("A", "Entregado", "c2", Fecha.AddDays(1));
            sheet.DiasLibres = 10;

            var c = _sut.DetectChanges(new List<ApplicationDataSheet> { sheet }, Snapshots(Snap("A", 1, "Pendiente", "c1", Fecha))).Should().ContainSingle().Subject;

            c.StateChanged.Should().BeTrue();
            c.CommentChanged.Should().BeTrue();
            c.OtherInfoChanged.Should().BeTrue();
        }

        [Fact]
        public void DetectChanges_DocumentoNuevoConOtraInformacion_NoMarcaOtherInfoChanged()
        {
            // Un documento nuevo no tiene "valor anterior" contra el cual comparar: su alta solo se
            // notifica por estado y/o comentario (aquí, ninguno de los dos => no hay cambio).
            var sheet = Sheet("NUEVO", "", "");
            sheet.Transportista = "TCC";
            sheet.ValorAnticipo = 100m;
            sheet.FechaEta = new DateTime(2024, 5, 5);

            _sut.DetectChanges(new List<ApplicationDataSheet> { sheet }, Snapshots()).Should().BeEmpty();
        }

        [Fact]
        public void DetectChanges_DocumentoNuevoConEstadoYOtraInformacion_SoloMarcaElEstado()
        {
            var sheet = Sheet("NUEVO", "Pendiente");
            sheet.Transportista = "TCC";

            var c = _sut.DetectChanges(new List<ApplicationDataSheet> { sheet }, Snapshots()).Should().ContainSingle().Subject;

            c.IsNewDocument.Should().BeTrue();
            c.StateChanged.Should().BeTrue();
            c.OtherInfoChanged.Should().BeFalse();
        }

        // ---------------------------------------------------------------- Origen / Destino / Comentario vigentes

        [Fact]
        public void DetectChanges_IncluyeOrigenDestinoYElComentarioVigentes()
        {
            var sheet = Sheet("A", "Entregado", "ultimo comentario", Fecha);
            sheet.Origen = "Medellin";
            sheet.Destino = "Miami";

            var c = _sut.DetectChanges(new List<ApplicationDataSheet> { sheet }, Snapshots(Snap("A", 1, "Pendiente", "ultimo comentario", Fecha))).Should().ContainSingle().Subject;

            c.Origen.Should().Be("Medellin");
            c.Destino.Should().Be("Miami");
            c.Comentario.Should().Be("ultimo comentario");
            c.FechaComentario.Should().Be(Fecha);
        }

        [Fact]
        public void DetectChanges_CambioDeComentario_TraeElComentarioYLaFechaNuevos()
        {
            var sheet = Sheet("A", "Pendiente", "nuevo", Fecha.AddDays(2));

            var c = _sut.DetectChanges(new List<ApplicationDataSheet> { sheet }, Snapshots(Snap("A", 1, "Pendiente", "viejo", Fecha))).Should().ContainSingle().Subject;

            c.CommentChanged.Should().BeTrue();
            c.Comentario.Should().Be("nuevo");
            c.FechaComentario.Should().Be(Fecha.AddDays(2));
        }

        [Fact]
        public void DetectChanges_CambioQueNoEsDeComentario_ConservaElComentarioYLaFechaExistentes()
        {
            var sheet = Sheet("A", "Pendiente", "existente", Fecha);
            sheet.DiasLibres = 4;

            var c = _sut.DetectChanges(new List<ApplicationDataSheet> { sheet }, Snapshots(Snap("A", 1, "Pendiente", "existente", Fecha))).Should().ContainSingle().Subject;

            c.CommentChanged.Should().BeFalse();
            c.OtherInfoChanged.Should().BeTrue();
            c.Comentario.Should().Be("existente");
            c.FechaComentario.Should().Be(Fecha);
        }
    }
}
