using Connection360.Domain.Dtos;
using Connection360.Domain.Enums;
using Connection360.Infrastructure.Persistence.Repositories;
using Connection360.Infrastructure.Tests.TestHelpers;
using FluentAssertions;
using Xunit;

namespace Connection360.Infrastructure.Tests.Persistence.Repositories
{
    public class LogStatusTrackingViewRepositoryTests
    {
        private static readonly String[] AllColumns =
            { "id", "id_operacion", "documento_transporte_hbl", "fecha_cambio", "usuario_cambio", "mensaje", "estado_anterior", "nuevo_estado" };

        private readonly FakeDbConnection _connection = new();
        private readonly LogStatusTrackingViewRepository _sut;

        public LogStatusTrackingViewRepositoryTests()
        {
            _sut = new LogStatusTrackingViewRepository(TestDbSession.Create(_connection));
        }

        private static Object?[] Row(Int64 id, String hbl) =>
            new Object?[] { id, 100L + id, hbl, new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), "user", "msg", "ANTES", "DESPUES" };

        [Fact]
        public async Task GetAllAsync_MapeaLasFilasAlDto()
        {
            _connection.ReaderResult = FakeDbConnection.Table(AllColumns, Row(1, "HBL-1"), Row(2, "HBL-2"));

            List<LogStatusTrackingViewResultDto> result = await _sut.GetAllAsync();

            result.Should().HaveCount(2);
            LogStatusTrackingViewResultDto first = result[0];
            first.Id.Should().Be(1);
            first.IdOperacion.Should().Be(101);
            first.DocumentoTransporteHbl.Should().Be("HBL-1");
            first.FechaCambio.Should().Be(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc));
            first.UsuarioCambio.Should().Be("user");
            first.Mensaje.Should().Be("msg");
            first.EstadoAnterior.Should().Be("ANTES");
            first.NuevoEstado.Should().Be("DESPUES");
        }

        [Fact]
        public async Task GetAllAsync_AbreLaConexionYUsaLaVistaConTodasLasColumnas()
        {
            _connection.ReaderResult = FakeDbConnection.Table(AllColumns);

            await _sut.GetAllAsync();

            _connection.OpenCount.Should().Be(1);
            _connection.Commands.Should().ContainSingle().Which.CommandText.Should().Be(
                "SELECT id, id_operacion, documento_transporte_hbl, fecha_cambio, usuario_cambio, mensaje, estado_anterior, nuevo_estado FROM connection360read.vw_log_status_tracking;");
        }

        [Fact]
        public async Task GetAllAsync_SinFilas_RetornaListaVacia()
        {
            _connection.ReaderResult = FakeDbConnection.Table(AllColumns);

            List<LogStatusTrackingViewResultDto> result = await _sut.GetAllAsync();

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllAsync_ConSeleccion_SoloConsultaLasColumnasPedidasEnOrdenFijo()
        {
            _connection.ReaderResult = FakeDbConnection.Table(new[] { "id", "mensaje" }, new Object?[] { 5L, "hola" });
            var selection = new LogStatusTrackingViewFieldsSelectionDto
            {
                Fields = { LogStatusTrackingViewField.Mensaje, LogStatusTrackingViewField.Id, LogStatusTrackingViewField.Id }
            };

            List<LogStatusTrackingViewResultDto> result = await _sut.GetAllAsync(selection);

            _connection.Commands.Single().CommandText.Should().Be("SELECT id, mensaje FROM connection360read.vw_log_status_tracking;");
            result.Should().ContainSingle();
            result[0].Id.Should().Be(5);
            result[0].Mensaje.Should().Be("hola");
            result[0].NuevoEstado.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllAsync_ConSeleccionNula_LanzaArgumentNullException()
        {
            Func<Task> act = () => _sut.GetAllAsync((LogStatusTrackingViewFieldsSelectionDto)null!);

            await act.Should().ThrowAsync<ArgumentNullException>();
            _connection.Commands.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllAsync_ConSeleccionSinCampos_LanzaArgumentException()
        {
            Func<Task> act = () => _sut.GetAllAsync(new LogStatusTrackingViewFieldsSelectionDto());

            await act.Should().ThrowAsync<ArgumentException>();
            _connection.Commands.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByDocumentoTransporteHblAsync_FiltraPorHblConParametro()
        {
            _connection.ReaderResult = FakeDbConnection.Table(AllColumns, Row(7, "HBL-7"));

            List<LogStatusTrackingViewResultDto> result = await _sut.GetByDocumentoTransporteHblAsync("HBL-7");

            result.Should().ContainSingle().Which.DocumentoTransporteHbl.Should().Be("HBL-7");
            RecordedCommand command = _connection.Commands.Single();
            command.CommandText.Should().EndWith("FROM connection360read.vw_log_status_tracking WHERE documento_transporte_hbl = @DocumentoTransporteHbl;");
            command.CommandText.Should().StartWith("SELECT id, id_operacion");
            command.Parameters.Should().ContainKey("DocumentoTransporteHbl").WhoseValue.Should().Be("HBL-7");
        }

        [Fact]
        public async Task GetByDocumentoTransporteHblAsync_ConSeleccion_UsaSoloLasColumnasPedidas()
        {
            _connection.ReaderResult = FakeDbConnection.Table(new[] { "fecha_cambio", "nuevo_estado" }, new Object?[] { new DateTime(2025, 5, 5), "FIN" });
            var selection = new LogStatusTrackingViewFieldsSelectionDto
            {
                Fields = { LogStatusTrackingViewField.NuevoEstado, LogStatusTrackingViewField.FechaCambio }
            };

            List<LogStatusTrackingViewResultDto> result = await _sut.GetByDocumentoTransporteHblAsync("HBL-9", selection);

            RecordedCommand command = _connection.Commands.Single();
            command.CommandText.Should().Be("SELECT fecha_cambio, nuevo_estado FROM connection360read.vw_log_status_tracking WHERE documento_transporte_hbl = @DocumentoTransporteHbl;");
            command.Parameters["DocumentoTransporteHbl"].Should().Be("HBL-9");
            result.Single().NuevoEstado.Should().Be("FIN");
        }

        [Fact]
        public async Task GetByDocumentoTransporteHblAsync_ConSeleccionNula_LanzaArgumentNullException()
        {
            Func<Task> act = () => _sut.GetByDocumentoTransporteHblAsync("HBL", null!);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task GetByDocumentoTransporteHblAsync_ConSeleccionSinCampos_LanzaArgumentException()
        {
            Func<Task> act = () => _sut.GetByDocumentoTransporteHblAsync("HBL", new LogStatusTrackingViewFieldsSelectionDto());

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GetAllAsync_PropagaLaTransaccionActivaDeLaSesion()
        {
            DbSession session = TestDbSession.Create(_connection);
            await session.EnsureConnectionOpenAsync();
            session.Transaction = await session.Connection.BeginTransactionAsync();
            var sut = new LogStatusTrackingViewRepository(session);
            _connection.ReaderResult = FakeDbConnection.Table(AllColumns);

            await sut.GetAllAsync();

            _connection.Commands.Single().Transaction.Should().BeSameAs(session.Transaction);
        }

        [Fact]
        public async Task GetAllAsync_SiLaConsultaFalla_PropagaLaExcepcion()
        {
            _connection.ExceptionToThrow = new InvalidOperationException("fallo bd");

            Func<Task> act = () => _sut.GetAllAsync();

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("fallo bd");
        }

        [Fact]
        public async Task GetAllAsync_ConTokenCancelado_LanzaOperationCanceledException()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Func<Task> act = () => _sut.GetAllAsync(cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
    }
}
