using Connection360.Domain.Dtos;

namespace Connection360.Domain.Ports.Persistence
{
    public interface ICustomersOfCollaboratorsRepository
    {
        /// <summary>Indica si ya existe la relación entre el cliente y el colaborador.</summary>
        Task<Boolean> ExistsAsync(Int64 idCustomer, Int64 idCollaborator, CancellationToken cancellationToken = default);
        Task<Int64> CrearAsync(Int64 idCustomer, Int64 idCollaborator, CancellationToken cancellationToken = default);
        Task<List<CustomersOfCollaboratorDtoResult>> ListCustomersByCollaboratorAsync(String idCollaborator, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lista todas las relaciones cliente - colaborador de connection360write.customers_of_collaborators,
        /// con la identificación del cliente (identificacionCustomer) y del colaborador (identificacionCollaborator).
        /// </summary>
        Task<List<CustomerCollaboratorRelationDtoResult>> ListAllAsync(CancellationToken cancellationToken = default);
    }
}
