using Connection360Notification.Application.DTOs;

namespace Connection360Notification.Application.Ports
{
    public interface IGetNotificationsUseCase
    {
        List<NotificationsListResponse> ExecuteGetNotificationsAllAsync(ClientSummaryRequest request, CancellationToken cancellationToken);
    }
}
