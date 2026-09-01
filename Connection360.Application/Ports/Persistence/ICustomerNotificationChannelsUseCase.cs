using Connection360.Application.DTOs.Persistence;

namespace Connection360.Application.Ports.Persistence
{
    public interface ICustomerNotificationChannelsUseCase
    {
        Task<IEnumerable<CustomerNotificationChannelsResponseDto>> ListAllAsync(CancellationToken cancellationToken = default);
        Task<CustomerNotificationChannelsResponseDto?> GetByIdAsync(Int64 id, CancellationToken cancellationToken = default);
        Task<CustomerNotificationChannelsResponseDto> CrearAsync(CreateCustomerNotificationChannelsDto dto, CancellationToken cancellationToken = default);
    }
}
