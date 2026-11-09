namespace Connection360.Domain.Dtos
{
    public class OutboxMessagesResultDto
    {
        public Guid Id { get; set; }
        public String EventType { get; set; } = String.Empty;
        public String Payload { get; set; } = String.Empty;
    }
}
