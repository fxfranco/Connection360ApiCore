namespace Connection360.Application.DTOs.Persistence
{
    public record CustomerNotificationChannelsResponseDto(Int64 IdNotificationChannels, Int64 IdCustomer, Boolean Application, Boolean Email, Boolean TextMessages);
}
