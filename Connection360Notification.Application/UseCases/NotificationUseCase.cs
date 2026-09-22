using Connection360Notification.Application.DTOs;
using Connection360Notification.Application.Ports.Inbound;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Ports.Outbound;

namespace Connection360Notification.Application.UseCases
{
    public class NotificationUseCase : INotificationUseCase
    {
        private readonly IKafkaProducerService _kafkaProducer;

        public NotificationUseCase(IKafkaProducerService kafkaProducer)
        {
            _kafkaProducer = kafkaProducer;
        }

        public async Task ExecuteSendAsync(CreateNotificationRequest request, CancellationToken cancellationToken)
        {
            var notification = new NotificationMessage(request.Recipient, request.Content, request.Type);

            // Se publica el mensaje en Kafka para procesamiento asíncrono
            await _kafkaProducer.ProduceNotificationAsync(notification, cancellationToken);
        }
    }
}
