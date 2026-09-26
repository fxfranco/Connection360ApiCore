namespace Connection360.Application.DTOs.Persistence
{
    public record CreateOutboxMessagesDto(String ClientId, String EventType, String DocumentNumber, String Title, String Message, DateTime? MessageDate = null) 
    {
        public DateTime? MessageDate { get; init; } = MessageDate ?? DateTime.Now;
    };
}
