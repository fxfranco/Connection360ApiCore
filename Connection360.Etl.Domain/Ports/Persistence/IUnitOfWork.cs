using System;
using System.Threading;
using System.Threading.Tasks;

namespace Connection360.Etl.Domain.Ports.Persistence
{
    /// <summary>
    /// Puerto de dominio para coordinar una transacción de PostgreSQL y resolver repositorios bajo
    /// demanda. Copiado de Connection360.Domain.Ports.Persistence.IUnitOfWork.
    /// </summary>
    public interface IUnitOfWork : IAsyncDisposable
    {
        // Retorna la interfaz del repositorio específico mediante resolución bajo demanda
        TRepository GetRepository<TRepository>() where TRepository : class;
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitAsync(CancellationToken cancellationToken = default);
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}
