using Connection360.Domain.Entities.Persistence;


namespace Connection360.Domain.Ports.Persistence
{
    public interface ICustomerRepository
    {
        Task<Int64?> GetCustomerByIdAsync(String id, CancellationToken cancellationToken = default);
        Task<Int64> CrearAsync(String id, CancellationToken cancellationToken = default);
    }
}
