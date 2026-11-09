namespace Connection360Notification.Domain.Settings
{
    public class KafkaSettings
    {
        public String BootstrapServers { get; set; } = String.Empty;
        public String GroupId { get; set; } = String.Empty;
        public String Topic { get; set; } = String.Empty;
    }
}
