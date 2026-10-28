using Connection360.Etl.Domain.Ports.Persistence;
using Connection360.Etl.Infrastructure.Persistence;
using Connection360.Etl.Infrastructure.Persistence.Repositories;
using Connection360.Etl.Infrastructure.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Data;
using Xunit;

namespace Connection360.Etl.Infrastructure.Tests.Persistence
{
    public class UnitOfWorkTests
    {
        private static UnitOfWork Create(FakeSessionScope scope, IServiceProvider? provider = null)
            => new(scope.Session, provider ?? new ServiceCollection().BuildServiceProvider());

        [Fact]
        public async Task GetRepository_RepositorioRegistrado_LoResuelveDesdeElContenedor()
        {
            await using var scope = new FakeSessionScope();
            var repository = Mock.Of<IOutboxMessageRepository>();
            var provider = new ServiceCollection().AddSingleton(repository).BuildServiceProvider();
            var unitOfWork = Create(scope, provider);

            var resolved = unitOfWork.GetRepository<IOutboxMessageRepository>();

            resolved.Should().BeSameAs(repository);
        }

        [Fact]
        public async Task GetRepository_RepositorioNoRegistrado_LanzaInvalidOperationException()
        {
            await using var scope = new FakeSessionScope();
            var unitOfWork = Create(scope);

            Action act = () => unitOfWork.GetRepository<IOutboxMessageRepository>();

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public async Task BeginTransactionAsync_AbreLaConexionCerradaYAsignaLaTransaccionALaSesion()
        {
            await using var scope = new FakeSessionScope(ConnectionState.Closed);
            var unitOfWork = Create(scope);

            await unitOfWork.BeginTransactionAsync();

            scope.Connection.OpenCalls.Should().Be(1);
            scope.Session.Transaction.Should().BeSameAs(scope.Connection.LastTransaction);
            scope.Session.Transaction.Should().NotBeNull();
        }

        [Fact]
        public async Task CommitAsync_ConTransaccionActiva_HaceCommitLaLiberaYLaLimpia()
        {
            await using var scope = new FakeSessionScope();
            var unitOfWork = Create(scope);
            await unitOfWork.BeginTransactionAsync();
            FakeDbTransaction transaction = scope.Connection.LastTransaction!;

            await unitOfWork.CommitAsync();

            transaction.Committed.Should().BeTrue();
            transaction.RolledBack.Should().BeFalse();
            transaction.Disposed.Should().BeTrue();
            scope.Session.Transaction.Should().BeNull();
        }

        [Fact]
        public async Task CommitAsync_SinTransaccion_NoHaceNada()
        {
            await using var scope = new FakeSessionScope();
            var unitOfWork = Create(scope);

            Func<Task> act = () => unitOfWork.CommitAsync();

            await act.Should().NotThrowAsync();
            scope.Connection.LastTransaction.Should().BeNull();
        }

        [Fact]
        public async Task RollbackAsync_ConTransaccionActiva_HaceRollbackLaLiberaYLaLimpia()
        {
            await using var scope = new FakeSessionScope();
            var unitOfWork = Create(scope);
            await unitOfWork.BeginTransactionAsync();
            FakeDbTransaction transaction = scope.Connection.LastTransaction!;

            await unitOfWork.RollbackAsync();

            transaction.RolledBack.Should().BeTrue();
            transaction.Committed.Should().BeFalse();
            transaction.Disposed.Should().BeTrue();
            scope.Session.Transaction.Should().BeNull();
        }

        [Fact]
        public async Task RollbackAsync_SinTransaccion_NoHaceNada()
        {
            await using var scope = new FakeSessionScope();
            var unitOfWork = Create(scope);

            Func<Task> act = () => unitOfWork.RollbackAsync();

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task CicloCompleto_DosTransaccionesSeguidas_CadaUnaTieneSuPropiaInstancia()
        {
            await using var scope = new FakeSessionScope();
            var unitOfWork = Create(scope);

            await unitOfWork.BeginTransactionAsync();
            FakeDbTransaction first = scope.Connection.LastTransaction!;
            await unitOfWork.CommitAsync();
            await unitOfWork.BeginTransactionAsync();
            FakeDbTransaction second = scope.Connection.LastTransaction!;
            await unitOfWork.RollbackAsync();

            second.Should().NotBeSameAs(first);
            first.Committed.Should().BeTrue();
            second.RolledBack.Should().BeTrue();
        }

        [Fact]
        public async Task DisposeAsync_LiberaLaSesionSubyacente()
        {
            await using var scope = new FakeSessionScope();
            var unitOfWork = Create(scope);
            await unitOfWork.BeginTransactionAsync();
            FakeDbTransaction transaction = scope.Connection.LastTransaction!;

            await unitOfWork.DisposeAsync();

            transaction.Disposed.Should().BeTrue();
            scope.Connection.Disposed.Should().BeTrue();
            scope.Session.Transaction.Should().BeNull();
        }
    }
}
