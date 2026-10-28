namespace Connection360Notification.Domain.Ports.Outbound
{
    public interface IKafkaProducerService
    {
        Task ProduceNotificationAsync(NotificationMessage notification, CancellationToken cancellationToken);
    }
}
