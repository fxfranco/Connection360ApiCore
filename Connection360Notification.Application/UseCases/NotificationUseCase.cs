using Connection360Notification.Application.DTOs;
using Connection360Notification.Application.Mapping;
using Connection360Notification.Application.Ports.Inbound;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Ports.Outbound;
using AppNotificationType = Connection360Notification.Application.Enum.NotificationType;

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
            // Se usa System.Enum.TryParse totalmente calificado (y no "Enum.TryParse") porque este
            // archivo vive bajo Connection360Notification.Application, donde también existe el
            // namespace Connection360Notification.Application.Enum: un "Enum" sin calificar
            // resolvería a ese namespace y no compilaría.
            if (!System.Enum.TryParse(request.Type, ignoreCase: true, out AppNotificationType parsedType))
            {
                throw new ArgumentException($"El tipo de notificación '{request.Type}' no es válido.", nameof(request.Type));
            }

            var notification = new NotificationMessage(
                clientId: request.Recipient,
                type: parsedType.ToDomain(),
                message: request.Content,
                documentNumber: request.DocumentNumber ?? String.Empty,
                title: request.Title ?? String.Empty,
                messageDate: request.MessageDate ?? default);

            // Se publica el mensaje en Kafka para procesamiento asíncrono
            await _kafkaProducer.ProduceNotificationAsync(notification, cancellationToken);
        }
    }
}
