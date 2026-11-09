using Connection360.Domain.Dtos;

namespace Connection360.Domain.Ports.Persistence
{
    public interface IOutboxMessagesRepository
    {
        Task<Guid> CrearAsync(OutboxMessagesRequestDto outboxMessages, CancellationToken cancellationToken = default);
        Task<List<OutboxMessagesResultDto>> GetListAsync(CancellationToken cancellationToken = default);
        Task<Boolean> UpdateprocessedAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
