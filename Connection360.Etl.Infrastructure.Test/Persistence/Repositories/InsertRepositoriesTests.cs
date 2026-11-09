using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Infrastructure.Persistence.Repositories;
using Connection360.Etl.Infrastructure.Tests.TestHelpers;
using FluentAssertions;
using System.Data;
using Xunit;

namespace Connection360.Etl.Infrastructure.Tests.Persistence.Repositories
{
    public class LogStatusTrackingRepositoryTests
    {
        private static LogStatusTracking Row(String doc) => new()
        {
            IdOperacion = 5,
            DocumentoTransporteHbl = doc,
            FechaCambio = new DateTime(2025, 2, 3, 4, 5, 6, DateTimeKind.Utc),
            UsuarioCambio = "USR",
            Mensaje = "MSG",
            EstadoAnterior = "A",
            NuevoEstado = "B",
        };

        [Fact]
        public async Task InsertBatchAsync_ListaVacia_DevuelveCeroSinTocarLaBaseDeDatos()
        {
            await using var scope = new FakeSessionScope(ConnectionState.Closed);
            var sut = new LogStatusTrackingRepository(scope.Session);

            Int32 result = await sut.InsertBatchAsync(new List<LogStatusTracking>());

            result.Should().Be(0);
            scope.Connection.Executions.Should().BeEmpty();
            scope.Connection.OpenCalls.Should().Be(0);
        }

        [Fact]
        public async Task InsertBatchAsync_Nulo_DevuelveCero()
        {
            await using var scope = new FakeSessionScope();
            var sut = new LogStatusTrackingRepository(scope.Session);

            Int32 result = await sut.InsertBatchAsync(null!);

            result.Should().Be(0);
            scope.Connection.Executions.Should().BeEmpty();
        }

        [Fact]
        public async Task InsertBatchAsync_ConFilas_EjecutaUnInsertPorFilaYSumaLasFilasAfectadas()
        {
            await using var scope = new FakeSessionScope(ConnectionState.Closed);
            var sut = new LogStatusTrackingRepository(scope.Session);

            Int32 result = await sut.InsertBatchAsync(new[] { Row("H1"), Row("H2"), Row("H3") });

            result.Should().Be(3);
            scope.Connection.OpenCalls.Should().Be(1);
            scope.Connection.Executions.Should().HaveCount(3);
            scope.Connection.Executions.Should().OnlyContain(e => e.CommandText.Contains("INSERT INTO connection360write.log_status_tracking"));
            scope.Connection.Executions.Select(e => e.Parameters["DocumentoTransporteHbl"]).Should().Equal("H1", "H2", "H3");
        }

        [Fact]
        public async Task InsertBatchAsync_MapeaTodasLasColumnasComoParametros()
        {
            await using var scope = new FakeSessionScope();
            var sut = new LogStatusTrackingRepository(scope.Session);
            LogStatusTracking row = Row("H1");

            await sut.InsertBatchAsync(new[] { row });

            FakeExecution execution = scope.Connection.Executions.Single();
            execution.Parameters["IdOperacion"].Should().Be(5L);
            execution.Parameters["FechaCambio"].Should().Be(row.FechaCambio);
            execution.Parameters["UsuarioCambio"].Should().Be("USR");
            execution.Parameters["Mensaje"].Should().Be("MSG");
            execution.Parameters["EstadoAnterior"].Should().Be("A");
            execution.Parameters["NuevoEstado"].Should().Be("B");
        }

        [Fact]
        public async Task InsertBatchAsync_UsaLaTransaccionActivaDeLaSesion()
        {
            await using var scope = new FakeSessionScope();
            var transaction = new FakeDbTransaction(scope.Connection, IsolationLevel.ReadCommitted);
            scope.Session.Transaction = transaction;
            var sut = new LogStatusTrackingRepository(scope.Session);

            await sut.InsertBatchAsync(new[] { Row("H1"), Row("H2") });

            scope.Connection.Executions.Should().OnlyContain(e => ReferenceEquals(e.Transaction, transaction));
        }

        [Fact]
        public async Task InsertBatchAsync_FallaLaBaseDeDatos_PropagaLaExcepcion()
        {
            await using var scope = new FakeSessionScope();
            scope.Connection.ThrowOnExecute = new InvalidOperationException("duplicado");
            var sut = new LogStatusTrackingRepository(scope.Session);

            Func<Task> act = () => sut.InsertBatchAsync(new[] { Row("H1") });

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("duplicado");
        }
    }

    public class OutboxMessageRepositoryTests
    {
        private static OutboxMessage Message(String payload) => new()
        {
            Id = Guid.NewGuid(),
            EventType = "ChangeState",
            Payload = payload,
            CreatedAt = new DateTime(2025, 2, 3, 4, 5, 6, DateTimeKind.Utc),
        };

        [Fact]
        public async Task InsertBatchAsync_ListaVacia_DevuelveCeroSinTocarLaBaseDeDatos()
        {
            await using var scope = new FakeSessionScope(ConnectionState.Closed);
            var sut = new OutboxMessageRepository(scope.Session);

            Int32 result = await sut.InsertBatchAsync(new List<OutboxMessage>());

            result.Should().Be(0);
            scope.Connection.Executions.Should().BeEmpty();
            scope.Connection.OpenCalls.Should().Be(0);
        }

        [Fact]
        public async Task InsertBatchAsync_Nulo_DevuelveCero()
        {
            await using var scope = new FakeSessionScope();
            var sut = new OutboxMessageRepository(scope.Session);

            (await sut.InsertBatchAsync(null!)).Should().Be(0);
        }

        [Fact]
        public async Task InsertBatchAsync_ConMensajes_EjecutaUnInsertPorMensajeYMapeaLosParametros()
        {
            await using var scope = new FakeSessionScope(ConnectionState.Closed);
            var sut = new OutboxMessageRepository(scope.Session);
            OutboxMessage first = Message("{\"a\":1}");
            OutboxMessage second = Message("{\"b\":2}");

            Int32 result = await sut.InsertBatchAsync(new[] { first, second });

            result.Should().Be(2);
            scope.Connection.OpenCalls.Should().Be(1);
            scope.Connection.Executions.Should().HaveCount(2);
            scope.Connection.Executions.Should().OnlyContain(e => e.CommandText.Contains("INSERT INTO connection360write.outbox_messages"));
            FakeExecution firstExecution = scope.Connection.Executions[0];
            firstExecution.Parameters["Id"].Should().Be(first.Id);
            firstExecution.Parameters["EventType"].Should().Be("ChangeState");
            firstExecution.Parameters["Payload"].Should().Be("{\"a\":1}");
            firstExecution.Parameters["CreatedAt"].Should().Be(first.CreatedAt);
            scope.Connection.Executions[1].Parameters["Payload"].Should().Be("{\"b\":2}");
        }

        [Fact]
        public async Task InsertBatchAsync_UsaLaTransaccionActivaDeLaSesion()
        {
            await using var scope = new FakeSessionScope();
            var transaction = new FakeDbTransaction(scope.Connection, IsolationLevel.ReadCommitted);
            scope.Session.Transaction = transaction;
            var sut = new OutboxMessageRepository(scope.Session);

            await sut.InsertBatchAsync(new[] { Message("x") });

            scope.Connection.Executions.Single().Transaction.Should().BeSameAs(transaction);
        }

        [Fact]
        public async Task InsertBatchAsync_FallaLaBaseDeDatos_PropagaLaExcepcion()
        {
            await using var scope = new FakeSessionScope();
            scope.Connection.ThrowOnExecute = new TimeoutException("lento");
            var sut = new OutboxMessageRepository(scope.Session);

            Func<Task> act = () => sut.InsertBatchAsync(new[] { Message("x") });

            await act.Should().ThrowAsync<TimeoutException>();
        }
    }
}
