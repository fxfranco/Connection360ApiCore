namespace Connection360.Domain.Dtos
{
    public class OutboxMessagesRequestDto
    {
        public String ClientId { get; set; } = String.Empty;
        public String EventType { get; set; } = String.Empty;
        public String DocumentNumber { get; set; } = String.Empty;
        public String Title { get; set; } = String.Empty;
        public String Message { get; set; } = String.Empty;
        public DateTime? MessageDate { get; set; }
    }
}
