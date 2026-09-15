using Connection360.Domain.Dtos;

namespace Connection360.Domain.Ports.Persistence
{
    public interface ICustomersOfCollaboratorsRepository
    {
        Task<Int64> CrearAsync(Int64 idCustomer, Int64 idCollaborator, CancellationToken cancellationToken = default);
        Task<List<CustomersOfCollaboratorDtoResult>> ListCustomersByCollaboratorAsync(String idCollaborator, CancellationToken cancellationToken = default);
    }
}
