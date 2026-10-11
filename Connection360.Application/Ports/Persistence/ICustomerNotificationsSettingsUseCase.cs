using Connection360.Application.DTOs.Persistence;
using Connection360.Domain.Enums;

namespace Connection360.Application.Ports.Persistence
{
    /// <summary>
    /// Configuración de notificaciones. Con el rol <see cref="UserRoleApplication.CLIENT"/> gestiona canales y
    /// eventos del cliente; con ADMIN, ANALISTAOPE o ANALISTASAC gestiona únicamente los canales del colaborador
    /// (el identificador recibido en <c>ClientId</c> es la identificación del colaborador).
    /// </summary>
    public interface ICustomerNotificationsSettingsUseCase
    {
        Task<CustomerNotificationsSettingsResponse> GetCustomerNotificationSettings(String clientId, UserRoleApplication role, CancellationToken cancellationToken = default);
        Task<CustomerNotificationsSettingsResponse> CreateCustomerNotificationSettings(CustomerNotificationsSettingsResponse customerNotificationsSettings, UserRoleApplication role, CancellationToken cancellationToken = default);
        Task<Boolean> UpdateCustomerNotificationSettings(CustomerNotificationsSettingsResponse customerNotificationsSettings, UserRoleApplication role, CancellationToken cancellationToken = default);
    }
}
