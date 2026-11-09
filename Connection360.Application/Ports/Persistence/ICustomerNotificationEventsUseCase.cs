using Connection360.Application.DTOs.Persistence;

namespace Connection360.Application.Ports.Persistence
{
    public interface ICustomerNotificationEventsUseCase
    {
        Task<IEnumerable<CustomerNotificationEventsResponseDto>> ListAllAsync(CancellationToken cancellationToken = default);
        Task<CustomerNotificationEventsResponseDto?> GetByIdAsync(Int64 id, CancellationToken cancellationToken = default);
        Task<CustomerNotificationEventsResponseDto> CrearAsync(CreateCustomerNotificationEventsDto dto, CancellationToken cancellationToken = default);
    }
}
