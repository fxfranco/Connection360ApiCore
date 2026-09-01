using Connection360.Domain.Entities.Persistence;

namespace Connection360.Domain.Ports.Persistence
{
    public interface ICustomerNotificationEventRepository
    {
        Task<IEnumerable<CustomerNotificationEvents>> ListAllAsync(CancellationToken cancellationToken = default);
        Task<CustomerNotificationEvents?> GetByIdAsync(Int64 id, CancellationToken cancellationToken = default);
        Task<Int32> CrearAsync(CustomerNotificationEvents CustomerNotificationChannels, CancellationToken cancellationToken = default);
    }
}
