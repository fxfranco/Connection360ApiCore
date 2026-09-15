namespace Connection360.Domain.Ports.Persistence
{
    public interface ICollaboratorRepository
    {
        Task<Int64?> GetCollaboratorByIdAsync(String id, CancellationToken cancellationToken = default);
        Task<Int64> CrearAsync(String id, CancellationToken cancellationToken = default);
    }
}
