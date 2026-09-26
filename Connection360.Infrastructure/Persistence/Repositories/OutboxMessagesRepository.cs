using Connection360.Domain.Dtos;
using Connection360.Domain.Ports.Persistence;
using Dapper;
using System.Text.Json;

namespace Connection360.Infrastructure.Persistence.Repositories
{
    public class OutboxMessagesRepository : IOutboxMessagesRepository
    {
        private readonly DbSession _session;

        public OutboxMessagesRepository(DbSession session)
        {
            _session = session;
        }
        public async Task<Guid> CrearAsync(OutboxMessagesRequestDto outboxMessagesRequest, CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);
            var outboxMessage = new
            {
                Id = Guid.NewGuid(),
                EventType = outboxMessagesRequest.EventType,
                Payload = JsonSerializer.Serialize(outboxMessagesRequest),
                CreatedAt = DateTime.UtcNow
            };

            const string query = @"
            INSERT INTO connection360write.outbox_messages (id, event_type, payload, created_at) 
            VALUES (@Id, @EventType, @Payload, @CreatedAt) 
            RETURNING id;";

            var command = new CommandDefinition(
                query,
                new { outboxMessage.Id, outboxMessage.EventType, outboxMessage.Payload, outboxMessage.CreatedAt},
                transaction: _session.Transaction,
                cancellationToken: cancellationToken
            );

            return await _session.Connection.ExecuteScalarAsync<Guid>(command);
        }

        public async Task<List<OutboxMessagesResultDto>> GetListAsync(CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);
            const string query = @"SELECT id, event_type, payload 
                    FROM connection360write.outbox_messages 
                    WHERE processed_at IS NULL 
                    ORDER BY created_at ASC 
                    LIMIT 20
                    FOR UPDATE SKIP LOCKED;";

            var command = new CommandDefinition(
                query,
                new { },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken
            );

            var result = await _session.Connection.QueryAsync<OutboxMessagesResultDto>(command);
            return result.ToList();
        }

        public async Task<Boolean> UpdateprocessedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);

            const string query = @" UPDATE connection360write.outbox_messages SET processed_at = @Now WHERE id = @Id;";

            var command = new CommandDefinition(
                query,
                 new { Now = DateTime.UtcNow, Id = id },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken
            );

            Int16 rowsAffected = (Int16)await _session.Connection.ExecuteAsync(command);
            return rowsAffected > 0;
        }
    }
}
