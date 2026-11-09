using Connection360.Domain.Dtos;

namespace Connection360.Application.Ports.Persistence
{
    public interface ICollaboratorUseCase
    {
        Task<Int64> CreateAsync(String collaboratorId, CancellationToken cancellationToken = default);
        Task<Int64> CreateCustomerCollaboratorAsync(String customerId, String collaboratorId, CancellationToken cancellationToken = default);

        /// <summary>Lista todos los colaboradores registrados (connection360write.collaborators).</summary>
        Task<List<CollaboratorDtoResult>> ListAsync(CancellationToken cancellationToken = default);

        /// <summary>Lista todas las relaciones cliente - colaborador (connection360write.customers_of_collaborators).</summary>
        Task<List<CustomerCollaboratorRelationDtoResult>> ListCustomerCollaboratorAsync(CancellationToken cancellationToken = default);
    }
}
