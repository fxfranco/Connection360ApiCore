using Connection360.Domain.Entities.Persistence;

namespace Connection360.Domain.Ports.Persistence
{
    public interface ICustomerNotificationChannelsRepository
    {
        Task<IEnumerable<CustomerNotificationChannels>> ListAllAsync(CancellationToken cancellationToken = default);
        Task<CustomerNotificationChannels?> GetByIdAsync(Int64 id, CancellationToken cancellationToken = default);
        Task<Int32> CrearAsync(CustomerNotificationChannels CustomerNotificationChannels, CancellationToken cancellationToken = default);
    }
}
