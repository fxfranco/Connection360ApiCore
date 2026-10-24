using Connection360.Domain.Dtos;

namespace Connection360.Domain.Ports.Persistence
{
    public interface ICollaboratorRepository
    {
        Task<Int64?> GetCollaboratorByIdAsync(String id, CancellationToken cancellationToken = default);
        Task<Int64> CrearAsync(String id, CancellationToken cancellationToken = default);

        /// <summary>Lista todos los colaboradores de connection360write.collaborators (campo identificacion).</summary>
        Task<List<CollaboratorDtoResult>> ListAsync(CancellationToken cancellationToken = default);
    }
}
