using Connection360.Etl.Infrastructure.Persistence.Repositories;
using Connection360.Etl.Infrastructure.Tests.TestHelpers;
using FluentAssertions;
using Npgsql;
using System.Data;
using Xunit;

namespace Connection360.Etl.Infrastructure.Tests.Persistence.Repositories
{
    public class DbSessionTests
    {
        [Fact]
        public async Task Constructor_CreaLaConexionDesdeElDataSourceSinAbrirla()
        {
            await using var dataSource = NpgsqlDataSource.Create("Host=localhost;Database=fake;Username=u;Password=p");
            await using var session = new DbSession(dataSource);

            session.Connection.Should().BeOfType<NpgsqlConnection>();
            session.Connection.State.Should().Be(ConnectionState.Closed);
            session.Transaction.Should().BeNull();
        }

        [Fact]
        public async Task EnsureConnectionOpenAsync_ConConexionCerrada_LaAbreUnaVez()
        {
            await using var scope = new FakeSessionScope(ConnectionState.Closed);

            await scope.Session.EnsureConnectionOpenAsync();
            await scope.Session.EnsureConnectionOpenAsync();

            scope.Connection.OpenCalls.Should().Be(1);
            scope.Connection.State.Should().Be(ConnectionState.Open);
        }

        [Fact]
        public async Task EnsureConnectionOpenAsync_ConConexionYaAbierta_NoLaVuelveAAbrir()
        {
            await using var scope = new FakeSessionScope(ConnectionState.Open);

            await scope.Session.EnsureConnectionOpenAsync();

            scope.Connection.OpenCalls.Should().Be(0);
        }

        [Fact]
        public async Task Transaction_SePuedeAsignarYLeer()
        {
            await using var scope = new FakeSessionScope();
            var transaction = new FakeDbTransaction(scope.Connection, IsolationLevel.ReadCommitted);

            scope.Session.Transaction = transaction;

            scope.Session.Transaction.Should().BeSameAs(transaction);
        }

        [Fact]
        public async Task DisposeAsync_ConTransaccionActiva_LaLiberaYCierraLaConexion()
        {
            await using var scope = new FakeSessionScope();
            var transaction = new FakeDbTransaction(scope.Connection, IsolationLevel.ReadCommitted);
            scope.Session.Transaction = transaction;

            await scope.Session.DisposeAsync();

            transaction.Disposed.Should().BeTrue();
            scope.Session.Transaction.Should().BeNull();
            scope.Connection.CloseCalls.Should().Be(1);
            scope.Connection.Disposed.Should().BeTrue();
        }

        [Fact]
        public async Task DisposeAsync_SinTransaccion_CierraYLiberaLaConexion()
        {
            await using var scope = new FakeSessionScope();

            await scope.Session.DisposeAsync();

            scope.Connection.CloseCalls.Should().Be(1);
            scope.Connection.Disposed.Should().BeTrue();
        }

        [Fact]
        public async Task DisposeAsync_ConConexionRealNuncaAbierta_NoLanza()
        {
            await using var dataSource = NpgsqlDataSource.Create("Host=localhost;Database=fake;Username=u;Password=p");
            var session = new DbSession(dataSource);

            Func<Task> act = async () => await session.DisposeAsync();

            await act.Should().NotThrowAsync();
        }
    }
}
