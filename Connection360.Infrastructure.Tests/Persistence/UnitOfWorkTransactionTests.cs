using Connection360.Domain.Ports.Persistence;
using Connection360.Infrastructure.Persistence;
using Connection360.Infrastructure.Persistence.Repositories;
using Connection360.Infrastructure.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Connection360.Infrastructure.Tests.Persistence
{
    /// <summary>
    /// Ciclo de transaccion de <see cref="UnitOfWork"/> usando una DbSession cuya conexion es un fake
    /// en memoria (sin base de datos real).
    /// </summary>
    public class UnitOfWorkTransactionTests
    {
        private readonly FakeDbConnection _connection = new();
        private readonly DbSession _session;
        private readonly UnitOfWork _sut;

        public UnitOfWorkTransactionTests()
        {
            _session = TestDbSession.Create(_connection);
            _sut = new UnitOfWork(_session, new ServiceCollection().BuildServiceProvider());
        }

        [Fact]
        public async Task BeginTransactionAsync_AbreLaConexionYAsignaLaTransaccionALaSesion()
        {
            await _sut.BeginTransactionAsync();

            _connection.OpenCount.Should().Be(1);
            _session.Transaction.Should().NotBeNull().And.BeSameAs(_connection.LastTransaction);
        }

        [Fact]
        public async Task CommitAsync_ConTransaccionActiva_HaceCommitLaLiberaYLaLimpia()
        {
            await _sut.BeginTransactionAsync();
            FakeDbTransaction transaction = _connection.LastTransaction!;

            await _sut.CommitAsync();

            transaction.Committed.Should().BeTrue();
            transaction.RolledBack.Should().BeFalse();
            transaction.Disposed.Should().BeTrue();
            _session.Transaction.Should().BeNull();
        }

        [Fact]
        public async Task CommitAsync_SinTransaccion_NoHaceNada()
        {
            Func<Task> act = () => _sut.CommitAsync();

            await act.Should().NotThrowAsync();
            _connection.LastTransaction.Should().BeNull();
        }

        [Fact]
        public async Task RollbackAsync_ConTransaccionActiva_HaceRollbackLaLiberaYLaLimpia()
        {
            await _sut.BeginTransactionAsync();
            FakeDbTransaction transaction = _connection.LastTransaction!;

            await _sut.RollbackAsync();

            transaction.RolledBack.Should().BeTrue();
            transaction.Committed.Should().BeFalse();
            transaction.Disposed.Should().BeTrue();
            _session.Transaction.Should().BeNull();
        }

        [Fact]
        public async Task RollbackAsync_SinTransaccion_NoHaceNada()
        {
            Func<Task> act = () => _sut.RollbackAsync();

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task CommitAsync_DespuesDeCommit_EsIdempotente()
        {
            await _sut.BeginTransactionAsync();
            await _sut.CommitAsync();

            Func<Task> act = () => _sut.CommitAsync();

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task BeginTransactionAsync_ConTokenCancelado_LanzaOperationCanceledException()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Func<Task> act = () => _sut.BeginTransactionAsync(cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
            _session.Transaction.Should().BeNull();
        }

        [Fact]
        public async Task DisposeAsync_LiberaLaSesionConSuTransaccionYConexion()
        {
            await _sut.BeginTransactionAsync();
            FakeDbTransaction transaction = _connection.LastTransaction!;

            await _sut.DisposeAsync();

            transaction.Disposed.Should().BeTrue();
            _connection.Disposed.Should().BeTrue();
            _connection.CloseCount.Should().Be(1);
        }

        [Fact]
        public async Task Repositorios_ResueltosPorElUnitOfWork_ComparteLaTransaccionDeLaSesion()
        {
            // Los repositorios reciben la misma DbSession (DI scoped), por lo que ven la transaccion abierta por el UnitOfWork.
            var services = new ServiceCollection();
            services.AddSingleton(_session);
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            var uow = new UnitOfWork(_session, services.BuildServiceProvider());
            _connection.ScalarResult = 12L;

            await uow.BeginTransactionAsync();
            Int64 id = await uow.GetRepository<ICustomerRepository>().CrearAsync("900123");
            await uow.CommitAsync();

            id.Should().Be(12);
            _connection.Commands.Single().Transaction.Should().NotBeNull();
        }

        [Fact]
        public void GetRepository_ConMock_ResuelveElMismoObjeto()
        {
            var repo = new Mock<IOutboxMessagesRepository>().Object;
            var services = new ServiceCollection().AddSingleton(repo).BuildServiceProvider();
            var uow = new UnitOfWork(_session, services);

            uow.GetRepository<IOutboxMessagesRepository>().Should().BeSameAs(repo);
        }
    }
}
