namespace Connection360.Application.DTOs.Persistence
{
    public record CreateCustomerNotificationChannelsDto(Int64 IdCustomer, Boolean Application, Boolean Email, Boolean TextMessages);
}
