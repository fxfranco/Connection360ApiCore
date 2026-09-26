using Connection360.Application.DTOs.Persistence;

namespace Connection360.Application.Ports.Persistence
{
    public interface IOutboxMessagesUseCase
    {
        Task<Boolean> CreateAsync(CreateOutboxMessagesDto OutboxMessages, CancellationToken cancellationToken = default);
    }
}
