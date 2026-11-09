using Connection360.Etl.Domain.Ports.Persistence;
using Connection360.Etl.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Connection360.Etl.Infrastructure.Persistence
{
    /// <summary>
    /// Copiado de Connection360.Infrastructure.Persistence.UnitOfWork. Nota: a diferencia de la API
    /// (donde el ciclo de vida SCOPED coincide con una petición HTTP), aquí el scope lo crea
    /// explícitamente Connection360.Etl.App alrededor de cada corrida completa del proceso ETL
    /// (ver Program.cs) usando <c>IServiceScopeFactory.CreateAsyncScope()</c>, ya que
    /// <see cref="UnitOfWork"/> solo implementa <see cref="IAsyncDisposable"/> (ver la lección
    /// aprendida con Connection360.Infrastructure.Messaging.OutboxPublisherWorker: liberar un scope
    /// que resuelve un servicio solo-async con un `using` síncrono lanza InvalidOperationException).
    /// </summary>
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

        public async ValueTask DisposeAsync()
        {
            // El DisposeAsync del DbSession se encargará de liberar la transacción y conexión
            await _session.DisposeAsync();
        }
    }
}
