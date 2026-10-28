using Connection360.Domain.Dtos;

namespace Connection360.Application.Ports.Persistence
{
    public interface ICustomerUseCase
    {
        Task<Int64?> GetByIdAsync(String clientId, CancellationToken cancellationToken = default);
        Task<Int64> CrearAsync(String clientId, CancellationToken cancellationToken = default);

        /// <summary>Lista todos los clientes registrados (connection360write.customers).</summary>
        Task<List<CustomerDtoResult>> ListAsync(CancellationToken cancellationToken = default);
    }
}
