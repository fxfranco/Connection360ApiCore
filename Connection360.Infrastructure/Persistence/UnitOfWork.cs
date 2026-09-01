using Connection360.Domain.Ports.Persistence;
using Connection360.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Connection360.Infrastructure.Persistence
{
    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly DbSession _session;
        private readonly IServiceProvider _serviceProvider;

        public UnitOfWork(DbSession session, IServiceProvider serviceProvider)
        {
            _session = session;
            _serviceProvider = serviceProvider;
        }

        public TRepository GetRepository<TRepository>() where TRepository : class
        {
            // Resuelve el repositorio desde el contenedor DI de forma 'Lazy' (solo al llamarlo)
            return _serviceProvider.GetRequiredService<TRepository>();
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);
            _session.Transaction = await _session.Connection.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_session.Transaction != null)
            {
                await _session.Transaction.CommitAsync(cancellationToken);
                await _session.Transaction.DisposeAsync();
                _session.Transaction = null;
            }
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_session.Transaction != null)
            {
                await _session.Transaction.RollbackAsync(cancellationToken);
                await _session.Transaction.DisposeAsync();
                _session.Transaction = null;
            }
        }

        // Al finalizar el Request (Scoped lifetime), se invoca DisposeAsync automáticamente
        public async ValueTask DisposeAsync()
        {
            // El DisposeAsync del DbSession se encargará de liberar la transacción y conexión
            await _session.DisposeAsync();
        }
    }
}
