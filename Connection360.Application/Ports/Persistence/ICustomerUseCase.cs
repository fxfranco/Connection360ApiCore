namespace Connection360.Application.Ports.Persistence
{
    public interface ICustomerUseCase
    {
        Task<Int64?> GetByIdAsync(String clientId, CancellationToken cancellationToken = default);
        Task<Int64> CrearAsync(String clientId, CancellationToken cancellationToken = default);
    }
}
