using Connection360.Domain.Dtos;
using Connection360.Domain.Entities.Persistence;


namespace Connection360.Domain.Ports.Persistence
{
    public interface ICustomerRepository
    {
        Task<Int64?> GetCustomerByIdAsync(String id, CancellationToken cancellationToken = default);
        Task<Int64> CrearAsync(String id, CancellationToken cancellationToken = default);

        /// <summary>Lista todos los clientes de connection360write.customers (campo identificacion).</summary>
        Task<List<CustomerDtoResult>> ListAsync(CancellationToken cancellationToken = default);
    }
}
