using Connection360Notification.Application.DTOs;
using Connection360Notification.Application.Mapping;
using Connection360Notification.Application.Ports;
using Connection360Notification.Domain.Ports.Outbound;

namespace Connection360Notification.Application.UseCases
{
    public class GetNotificationsUseCase : IGetNotificationsUseCase
    {
        private readonly INotificationRepository _notificationRepository;

        public GetNotificationsUseCase(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<List<NotificationsListResponse>> ExecuteGetNotificationsByClientAsync(ClientSummaryRequest request, CancellationToken cancellationToken)
        {
            if (request == null || String.IsNullOrWhiteSpace(request.IdClient))
            {
                return new List<NotificationsListResponse>();
            }

            var notifications = await _notificationRepository.GetByClientAsync(request.IdClient, cancellationToken);

            return notifications
                .Select(n => n.ToListResponse())
                .OrderByDescending(n => n.NotificationDate)
                .ToList();
        }

        public async Task<Boolean> ExecuteMarkAsReadAsync(NotificationsRequest request, CancellationToken cancellationToken)
        {
            if (request == null || String.IsNullOrWhiteSpace(request.IdClient))
            {
                return false;
            }

            return await _notificationRepository.MarkAsReadAsync(request.IdClient, request.IdNotification, cancellationToken);
        }

        public async Task<List<NotificationsListResponse>> ExecuteGetNotificationsAllAsync(CancellationToken cancellationToken)
        {
            var notifications = await _notificationRepository.GetAllAsync(cancellationToken);

            return notifications
                .Select(n => n.ToListResponse())
                .OrderByDescending(n => n.NotificationDate)
                .ToList();
        }
    }
}
