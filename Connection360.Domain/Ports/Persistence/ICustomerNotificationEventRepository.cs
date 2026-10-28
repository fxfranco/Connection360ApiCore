using Connection360.Domain.Entities.Persistence;

namespace Connection360.Domain.Ports.Persistence
{
    public interface ICustomerNotificationEventRepository
    {
        Task<IEnumerable<CustomerNotificationEvents>> ListAllAsync(CancellationToken cancellationToken = default);
        Task<CustomerNotificationEvents?> GetByIdAsync(Int64 id, CancellationToken cancellationToken = default);
        Task<CustomerNotificationEvents?> GetByCustomerIdAsync(Int64 customerId, CancellationToken cancellationToken = default);
        Task<Int64> CrearAsync(CustomerNotificationEvents customerNotificationEvents, CancellationToken cancellationToken = default);
        Task<Boolean> UpdateAsync(CustomerNotificationEvents customerNotificationEvents, CancellationToken cancellationToken = default);
    }
}
