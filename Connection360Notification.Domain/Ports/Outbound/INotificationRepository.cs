using System;
using System.Collections.Generic;
using System.Text;

namespace Connection360Notification.Domain.Ports.Outbound
{
    public interface INotificationRepository
    {
        Task SaveAsync(NotificationMessage notification, CancellationToken cancellationToken);
        Task<IEnumerable<NotificationMessage>> GetAllAsync(CancellationToken cancellationToken);
    }
}
