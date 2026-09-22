using Confluent.Kafka;
using Connection360Notification.Domain;
using Connection360Notification.Domain.Ports.Outbound;
using Connection360Notification.Domain.Settings;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Connection360Notification.Infrastructure.Messaging
{
    public class KafkaProducerService : IKafkaProducerService
    {
        private readonly IProducer<String, String> _producer;
        private readonly KafkaSettings _kafkaSettings;

        public KafkaProducerService(IOptions<KafkaSettings> kafkaSettings)
        {
            _kafkaSettings = kafkaSettings.Value;

            var config = new ProducerConfig
            {
                BootstrapServers = _kafkaSettings.BootstrapServers
            };

            _producer = new ProducerBuilder<String, String>(config).Build();
        }

        public async Task ProduceNotificationAsync(NotificationMessage notification, CancellationToken cancellationToken)
        {
            var message = new Message<String, String>
            {
                Key = notification.Id,
                Value = JsonSerializer.Serialize(notification)
            };

            await _producer.ProduceAsync(_kafkaSettings.Topic, message, cancellationToken);
        }
    }
}
