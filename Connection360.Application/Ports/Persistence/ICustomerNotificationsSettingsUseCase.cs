using Connection360.Application.DTOs.Persistence;

namespace Connection360.Application.Ports.Persistence
{
    public interface ICustomerNotificationsSettingsUseCase
    {
        Task<CustomerNotificationsSettingsResponse> GetCustomerNotificationSettings(String clientId, CancellationToken cancellationToken = default);
        Task<CustomerNotificationsSettingsResponse> CreateCustomerNotificationSettings(CustomerNotificationsSettingsResponse customerNotificationsSettings, CancellationToken cancellationToken = default);
        Task<Boolean> UpdateCustomerNotificationSettings(CustomerNotificationsSettingsResponse customerNotificationsSettings, CancellationToken cancellationToken = default);
    }
}
