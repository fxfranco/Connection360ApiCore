using System.Data;
using Connection360.Infrastructure.Persistence.Repositories;
using Connection360.Infrastructure.Tests.TestHelpers;
using FluentAssertions;
using Npgsql;
using Xunit;

namespace Connection360.Infrastructure.Tests.Persistence.Repositories
{
    public class DbSessionTests
    {
        [Fact]
        public async Task Constructor_CreaLaConexionDesdeElDataSourceSinAbrirla()
        {
            await using NpgsqlDataSource dataSource = NpgsqlDataSource.Create("Host=localhost;Database=test;Username=test;Password=test");

            await using var session = new DbSession(dataSource);

            session.Connection.Should().BeOfType<NpgsqlConnection>();
            session.Connection.State.Should().Be(ConnectionState.Closed);
            session.Transaction.Should().BeNull();
        }

        [Fact]
        public async Task EnsureConnectionOpenAsync_ConConexionCerrada_LaAbre()
        {
            var connection = new FakeDbConnection();
            DbSession session = TestDbSession.Create(connection);

            await session.EnsureConnectionOpenAsync();

            connection.State.Should().Be(ConnectionState.Open);
            connection.OpenCount.Should().Be(1);
        }

        [Fact]
        public async Task EnsureConnectionOpenAsync_ConConexionYaAbierta_NoLaVuelveAAbrir()
        {
            var connection = new FakeDbConnection();
            DbSession session = TestDbSession.Create(connection);

            await session.EnsureConnectionOpenAsync();
            await session.EnsureConnectionOpenAsync();
            await session.EnsureConnectionOpenAsync();

            connection.OpenCount.Should().Be(1);
        }

        [Fact]
        public async Task EnsureConnectionOpenAsync_ConTokenCancelado_LanzaOperationCanceledException()
        {
            // DbConnection.OpenAsync (por defecto) observa el token antes de delegar en Open().
            var connection = new FakeDbConnection();
            DbSession session = TestDbSession.Create(connection);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Func<Task> act = () => session.EnsureConnectionOpenAsync(cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
            connection.State.Should().Be(ConnectionState.Closed);
        }

        [Fact]
        public async Task Transaction_EsAsignableYSePuedeLimpiar()
        {
            var connection = new FakeDbConnection();
            DbSession session = TestDbSession.Create(connection);
            await session.EnsureConnectionOpenAsync();

            session.Transaction = await session.Connection.BeginTransactionAsync();
            session.Transaction.Should().BeSameAs(connection.LastTransaction);

            session.Transaction = null;
            session.Transaction.Should().BeNull();
        }

        [Fact]
        public async Task DisposeAsync_ConTransaccionYConexion_LosLiberaYCierra()
        {
            var connection = new FakeDbConnection();
            DbSession session = TestDbSession.Create(connection);
            await session.EnsureConnectionOpenAsync();
            session.Transaction = await session.Connection.BeginTransactionAsync();
            FakeDbTransaction transaction = connection.LastTransaction!;

            await session.DisposeAsync();

            transaction.Disposed.Should().BeTrue();
            session.Transaction.Should().BeNull();
            connection.CloseCount.Should().Be(1);
            connection.Disposed.Should().BeTrue();
        }

        [Fact]
        public async Task DisposeAsync_SinTransaccion_SoloCierraYLiberaLaConexion()
        {
            var connection = new FakeDbConnection();
            DbSession session = TestDbSession.Create(connection);

            await session.DisposeAsync();

            connection.CloseCount.Should().Be(1);
            connection.Disposed.Should().BeTrue();
        }

        [Fact]
        public async Task DisposeAsync_ConConexionNpgsqlReal_NoLanzaExcepcion()
        {
            await using NpgsqlDataSource dataSource = NpgsqlDataSource.Create("Host=localhost;Database=test;Username=test;Password=test");
            var session = new DbSession(dataSource);

            Func<Task> act = async () => await session.DisposeAsync();

            await act.Should().NotThrowAsync();
        }
    }
}
