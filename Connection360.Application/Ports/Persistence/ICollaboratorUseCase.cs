namespace Connection360.Application.Ports.Persistence
{
    public interface ICollaboratorUseCase
    {
        Task<Int64> CreateAsync(String collaboratorId, CancellationToken cancellationToken = default);
        Task<Int64> CreateCustomerCollaboratorAsync(String customerId, String collaboratorId, CancellationToken cancellationToken = default);
    }
}
