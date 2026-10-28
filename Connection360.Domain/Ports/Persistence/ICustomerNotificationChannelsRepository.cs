using Connection360.Domain.Entities.Persistence;

namespace Connection360.Domain.Ports.Persistence
{
    public interface ICustomerNotificationChannelsRepository
    {
        Task<IEnumerable<CustomerNotificationChannels>> ListAllAsync(CancellationToken cancellationToken = default);
        Task<CustomerNotificationChannels?> GetByIdAsync(Int64 id, CancellationToken cancellationToken = default);
        Task<CustomerNotificationChannels?> GetByCustomerIdAsync(Int64 customerId, CancellationToken cancellationToken = default);
        Task<Int64> CrearAsync(CustomerNotificationChannels customerNotificationChannels, CancellationToken cancellationToken = default);
        Task<Boolean> UpdateAsync(CustomerNotificationChannels customerNotificationChannels, CancellationToken cancellationToken = default);
    }
}
