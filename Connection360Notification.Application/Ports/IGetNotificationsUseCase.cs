using Connection360Notification.Application.DTOs;
using Connection360Notification.Domain;

namespace Connection360Notification.Application.Ports
{
    public interface IGetNotificationsUseCase
    {
        /// <summary>Lista las notificaciones de un cliente, más recientes primero.</summary>
        Task<List<NotificationsListResponse>> ExecuteGetNotificationsByClientAsync(ClientSummaryRequest request, CancellationToken cancellationToken);

        /// <summary>Marca como leída una notificación puntual del cliente. Devuelve false si no existe.</summary>
        Task<Boolean> ExecuteMarkAsReadAsync(NotificationsRequest request, CancellationToken cancellationToken);

        /// <summary>Lista las notificaciones de todos, más recientes primero.</summary>
        Task<List<NotificationsListResponse>> ExecuteGetNotificationsAllAsync(CancellationToken cancellationToken);
    }
}
