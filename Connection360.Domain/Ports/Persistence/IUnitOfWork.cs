namespace Connection360.Domain.Ports.Persistence
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        // Retorna la interfaz del repositorio específico mediante resolución bajo demanda
        TRepository GetRepository<TRepository>() where TRepository : class;
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitAsync(CancellationToken cancellationToken = default);
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}
