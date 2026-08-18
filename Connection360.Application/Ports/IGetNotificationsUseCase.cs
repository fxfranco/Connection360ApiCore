using Connection360.Application.DTOs;

namespace Connection360.Application.Ports
{
    public interface IGetNotificationsUseCase
    {
        List<NotificationsListResponse> ExecuteGetNotificationsAllAsync(ClientSummaryRequest request, CancellationToken cancellationToken);
    }
}
