using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Infrastructure.Persistence.Repositories;
using Connection360.Etl.Infrastructure.Tests.TestHelpers;
using FluentAssertions;
using System.Data;
using Xunit;

namespace Connection360.Etl.Infrastructure.Tests.Persistence.Repositories
{
    public class ApplicationDataSheetRepositoryTests
    {
        private static ApplicationDataSheet Sheet(String doc) => new()
        {
            DocumentoTransporteHbl = doc,
            NitCliente = "900",
            Cliente = "ACME",
            Estado = "EN TRANSITO",
            Comentario = "hola",
            FechaComentario = new DateTime(2025, 1, 2),
            PesoKg = 12.5m,
            CantidadBultos = 3,
            TipoContenedor = null,
            FechaDevolucionRealContenedor = null,
        };

        private static List<String> DocumentParameters(FakeExecution execution)
            => execution.Parameters.Where(p => p.Key.StartsWith("Documents", StringComparison.OrdinalIgnoreCase))
                .Select(p => (String)p.Value!).ToList();

        // ------------------------------------------------------------------ UpsertBatchAsync

        [Fact]
        public async Task UpsertBatchAsync_ListaVacia_DevuelveCeroSinTocarLaBaseDeDatos()
        {
            await using var scope = new FakeSessionScope(ConnectionState.Closed);
            var sut = new ApplicationDataSheetRepository(scope.Session);

            Int32 result = await sut.UpsertBatchAsync(new List<ApplicationDataSheet>());

            result.Should().Be(0);
            scope.Connection.Executions.Should().BeEmpty();
            scope.Connection.OpenCalls.Should().Be(0);
        }

        [Fact]
        public async Task UpsertBatchAsync_Nulo_DevuelveCero()
        {
            await using var scope = new FakeSessionScope();
            var sut = new ApplicationDataSheetRepository(scope.Session);

            (await sut.UpsertBatchAsync(null!)).Should().Be(0);
            scope.Connection.Executions.Should().BeEmpty();
        }

        [Fact]
        public async Task UpsertBatchAsync_ConFilas_EjecutaUnUpsertPorFilaYSumaLasAfectadas()
        {
            await using var scope = new FakeSessionScope(ConnectionState.Closed);
            scope.Connection.NonQueryResult = _ => 2;
            var sut = new ApplicationDataSheetRepository(scope.Session);

            Int32 result = await sut.UpsertBatchAsync(new[] { Sheet("H1"), Sheet("H2") });

            result.Should().Be(4);
            scope.Connection.OpenCalls.Should().Be(1);
            scope.Connection.Executions.Should().HaveCount(2);
            scope.Connection.Executions.Select(e => e.Parameters["DocumentoTransporteHbl"]).Should().Equal("H1", "H2");
        }

        [Fact]
        public async Task UpsertBatchAsync_UsaOnConflictPorDocumentoTransporteHbl()
        {
            await using var scope = new FakeSessionScope();
            var sut = new ApplicationDataSheetRepository(scope.Session);

            await sut.UpsertBatchAsync(new[] { Sheet("H1") });

            String sql = scope.Connection.Executions.Single().CommandText;
            sql.Should().Contain("INSERT INTO connection360write.application_data_sheet");
            sql.Should().Contain("ON CONFLICT (documento_transporte_hbl) DO UPDATE SET");
            sql.Should().Contain("comentario = EXCLUDED.comentario");
            sql.Should().Contain("estado = EXCLUDED.estado");
        }

        [Fact]
        public async Task UpsertBatchAsync_MapeaLasPropiedadesDeLaEntidadComoParametros()
        {
            await using var scope = new FakeSessionScope();
            var sut = new ApplicationDataSheetRepository(scope.Session);

            await sut.UpsertBatchAsync(new[] { Sheet("H1") });

            FakeExecution execution = scope.Connection.Executions.Single();
            execution.Parameters["NitCliente"].Should().Be("900");
            execution.Parameters["Cliente"].Should().Be("ACME");
            execution.Parameters["Estado"].Should().Be("EN TRANSITO");
            execution.Parameters["Comentario"].Should().Be("hola");
            execution.Parameters["FechaComentario"].Should().Be(new DateTime(2025, 1, 2));
            execution.Parameters["PesoKg"].Should().Be(12.5m);
            execution.Parameters["CantidadBultos"].Should().Be(3);
            execution.Parameters["TipoContenedor"].Should().BeNull();
            execution.Parameters["FechaDevolucionRealContenedor"].Should().BeNull();
        }

        [Fact]
        public async Task UpsertBatchAsync_UsaLaTransaccionActivaDeLaSesion()
        {
            await using var scope = new FakeSessionScope();
            var transaction = new FakeDbTransaction(scope.Connection, IsolationLevel.ReadCommitted);
            scope.Session.Transaction = transaction;
            var sut = new ApplicationDataSheetRepository(scope.Session);

            await sut.UpsertBatchAsync(new[] { Sheet("H1") });

            scope.Connection.Executions.Single().Transaction.Should().BeSameAs(transaction);
        }

        [Fact]
        public async Task UpsertBatchAsync_FallaLaBaseDeDatos_PropagaLaExcepcion()
        {
            await using var scope = new FakeSessionScope();
            scope.Connection.ThrowOnExecute = new InvalidOperationException("violacion");
            var sut = new ApplicationDataSheetRepository(scope.Session);

            Func<Task> act = () => sut.UpsertBatchAsync(new[] { Sheet("H1") });

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("violacion");
        }

        // ------------------------------------------------------------------ GetChangeSnapshotsAsync

        [Fact]
        public async Task GetChangeSnapshotsAsync_SinDocumentos_DevuelveDiccionarioVacioSinConsultar()
        {
            await using var scope = new FakeSessionScope(ConnectionState.Closed);
            var sut = new ApplicationDataSheetRepository(scope.Session);

            var result = await sut.GetChangeSnapshotsAsync(new List<String>());

            result.Should().BeEmpty();
            scope.Connection.Executions.Should().BeEmpty();
            scope.Connection.OpenCalls.Should().Be(0);
        }

        [Fact]
        public async Task GetChangeSnapshotsAsync_Nulo_DevuelveDiccionarioVacio()
        {
            await using var scope = new FakeSessionScope();
            var sut = new ApplicationDataSheetRepository(scope.Session);

            (await sut.GetChangeSnapshotsAsync(null!)).Should().BeEmpty();
        }

        [Fact]
        public async Task GetChangeSnapshotsAsync_SoloDocumentosVaciosOEspacios_NoConsulta()
        {
            await using var scope = new FakeSessionScope();
            var sut = new ApplicationDataSheetRepository(scope.Session);

            var result = await sut.GetChangeSnapshotsAsync(new[] { "", "   ", null! });

            result.Should().BeEmpty();
            scope.Connection.Executions.Should().BeEmpty();
        }

        [Fact]
        public async Task GetChangeSnapshotsAsync_FiltraVaciosYDuplicadosAntesDeConsultar()
        {
            await using var scope = new FakeSessionScope();
            scope.Connection.ReaderResult = _ => SnapshotTable();
            var sut = new ApplicationDataSheetRepository(scope.Session);

            await sut.GetChangeSnapshotsAsync(new[] { "H1", "H1", " ", "H2", "" });

            FakeExecution execution = scope.Connection.Executions.Single();
            execution.Kind.Should().Be("Reader");
            execution.CommandText.Should().Contain("FROM connection360write.application_data_sheet").And.Contain("fecha_comentario::timestamp");
            DocumentParameters(execution).Should().BeEquivalentTo(new[] { "H1", "H2" });
        }

        [Fact]
        public async Task GetChangeSnapshotsAsync_MapeaLasFilasYLasIndexaPorDocumentoConComparacionOrdinal()
        {
            await using var scope = new FakeSessionScope();
            scope.Connection.ReaderResult = _ => SnapshotTable(
                (1L, "H1", "ENTREGADO", "ok", new DateTime(2025, 5, 6)),
                (2L, "h2", "EN TRANSITO", "", new DateTime(2025, 5, 7)));
            var sut = new ApplicationDataSheetRepository(scope.Session);

            var result = await sut.GetChangeSnapshotsAsync(new[] { "H1", "h2" });

            result.Should().HaveCount(2);
            ApplicationDataSheetChangeSnapshot first = result["H1"];
            first.Id.Should().Be(1);
            first.DocumentoTransporteHbl.Should().Be("H1");
            first.Estado.Should().Be("ENTREGADO");
            first.Comentario.Should().Be("ok");
            first.FechaComentario.Should().Be(new DateTime(2025, 5, 6));
            result.ContainsKey("h1").Should().BeFalse("el diccionario usa comparación ordinal (sensible a mayúsculas)");
            result["h2"].Id.Should().Be(2);
        }

        [Fact]
        public async Task GetChangeSnapshotsAsync_SinFilasEnLaBaseDeDatos_DevuelveDiccionarioVacio()
        {
            await using var scope = new FakeSessionScope();
            scope.Connection.ReaderResult = _ => SnapshotTable();
            var sut = new ApplicationDataSheetRepository(scope.Session);

            var result = await sut.GetChangeSnapshotsAsync(new[] { "H1" });

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetChangeSnapshotsAsync_ConexionCerradaYTransaccion_AbreYUsaLaTransaccion()
        {
            await using var scope = new FakeSessionScope(ConnectionState.Closed);
            scope.Connection.ReaderResult = _ => SnapshotTable();
            var transaction = new FakeDbTransaction(scope.Connection, IsolationLevel.ReadCommitted);
            scope.Session.Transaction = transaction;
            var sut = new ApplicationDataSheetRepository(scope.Session);

            await sut.GetChangeSnapshotsAsync(new[] { "H1" });

            scope.Connection.OpenCalls.Should().Be(1);
            scope.Connection.Executions.Single().Transaction.Should().BeSameAs(transaction);
        }

        // ------------------------------------------------------------------ GetIdsByDocumentAsync

        [Fact]
        public async Task GetIdsByDocumentAsync_SinDocumentosValidos_DevuelveDiccionarioVacioSinConsultar()
        {
            await using var scope = new FakeSessionScope(ConnectionState.Closed);
            var sut = new ApplicationDataSheetRepository(scope.Session);

            (await sut.GetIdsByDocumentAsync(new[] { " ", "" })).Should().BeEmpty();
            (await sut.GetIdsByDocumentAsync(null!)).Should().BeEmpty();
            (await sut.GetIdsByDocumentAsync(new List<String>())).Should().BeEmpty();

            scope.Connection.Executions.Should().BeEmpty();
            scope.Connection.OpenCalls.Should().Be(0);
        }

        [Fact]
        public async Task GetIdsByDocumentAsync_DevuelveElIdPorDocumento()
        {
            await using var scope = new FakeSessionScope();
            scope.Connection.ReaderResult = _ => IdTable((10L, "H1"), (20L, "H2"));
            var sut = new ApplicationDataSheetRepository(scope.Session);

            var result = await sut.GetIdsByDocumentAsync(new[] { "H1", "H2", "H2", "" });

            result.Should().HaveCount(2);
            result["H1"].Should().Be(10);
            result["H2"].Should().Be(20);
            FakeExecution execution = scope.Connection.Executions.Single();
            execution.CommandText.Should().Contain("SELECT id AS \"Id\"").And.Contain("ANY(");
            DocumentParameters(execution).Should().BeEquivalentTo(new[] { "H1", "H2" });
        }

        [Fact]
        public async Task GetIdsByDocumentAsync_UsaLaTransaccionActivaYComparacionOrdinal()
        {
            await using var scope = new FakeSessionScope(ConnectionState.Closed);
            scope.Connection.ReaderResult = _ => IdTable((10L, "H1"));
            var transaction = new FakeDbTransaction(scope.Connection, IsolationLevel.ReadCommitted);
            scope.Session.Transaction = transaction;
            var sut = new ApplicationDataSheetRepository(scope.Session);

            var result = await sut.GetIdsByDocumentAsync(new[] { "H1" });

            scope.Connection.OpenCalls.Should().Be(1);
            scope.Connection.Executions.Single().Transaction.Should().BeSameAs(transaction);
            result.ContainsKey("h1").Should().BeFalse();
        }

        [Fact]
        public async Task GetIdsByDocumentAsync_FallaLaBaseDeDatos_PropagaLaExcepcion()
        {
            await using var scope = new FakeSessionScope();
            scope.Connection.ThrowOnExecute = new InvalidOperationException("sin conexion");
            var sut = new ApplicationDataSheetRepository(scope.Session);

            Func<Task> act = () => sut.GetIdsByDocumentAsync(new[] { "H1" });

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        // ------------------------------------------------------------------ helpers

        private static DataTable SnapshotTable(params (Int64 Id, String Doc, String Estado, String Comentario, DateTime Fecha)[] rows)
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Int64));
            table.Columns.Add("DocumentoTransporteHbl", typeof(String));
            table.Columns.Add("Estado", typeof(String));
            table.Columns.Add("Comentario", typeof(String));
            table.Columns.Add("FechaComentario", typeof(DateTime));
            foreach (var row in rows)
                table.Rows.Add(row.Id, row.Doc, row.Estado, row.Comentario, row.Fecha);
            return table;
        }

        private static DataTable IdTable(params (Int64 Id, String Doc)[] rows)
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Int64));
            table.Columns.Add("DocumentoTransporteHbl", typeof(String));
            foreach (var row in rows)
                table.Rows.Add(row.Id, row.Doc);
            return table;
        }
    }
}
