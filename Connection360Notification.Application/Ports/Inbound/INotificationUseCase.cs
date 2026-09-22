using Connection360Notification.Application.DTOs;

namespace Connection360Notification.Application.Ports.Inbound
{
    public interface INotificationUseCase
    {
        Task ExecuteSendAsync(CreateNotificationRequest request, CancellationToken cancellationToken);
    }
}
