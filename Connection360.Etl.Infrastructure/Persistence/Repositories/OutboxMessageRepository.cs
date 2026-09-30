using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Domain.Ports.Persistence;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Connection360.Etl.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Adaptador de <see cref="IOutboxMessageRepository"/>: inserta filas en PostgreSQL, tabla
    /// connection360write.outbox_messages (ver Documents/scriptoutboxmessagesSQL.sql). Mismo patrón
    /// Dapper + <see cref="DbSession"/> que <see cref="LogStatusTrackingRepository"/>: INSERT simple
    /// (sin ON CONFLICT), ya que la tabla no define ninguna columna UNIQUE de negocio (solo "id",
    /// que esta clase genera en la capa de aplicación antes de insertar). "processed_at" se deja
    /// explícitamente NULL: lo completa después Connection360.Infrastructure.Messaging.
    /// OutboxPublisherWorker (proceso de la API principal) al publicar el mensaje a Kafka.
    /// </summary>
    public class OutboxMessageRepository : IOutboxMessageRepository
    {
        private readonly DbSession _session;

        public OutboxMessageRepository(DbSession session)
        {
            _session = session;
        }

        public async Task<Int32> InsertBatchAsync(IEnumerable<OutboxMessage> messages, CancellationToken cancellationToken = default)
        {
            var messagesList = messages?.ToList() ?? new List<OutboxMessage>();
            if (messagesList.Count == 0)
                return 0;

            await _session.EnsureConnectionOpenAsync(cancellationToken);

            const string query = @"
                INSERT INTO connection360write.outbox_messages
                    (id, event_type, payload, created_at, processed_at)
                VALUES
                    (@Id, @EventType, @Payload, @CreatedAt, NULL);";

            var command = new CommandDefinition(
                query,
                messagesList,
                transaction: _session.Transaction,
                cancellationToken: cancellationToken);

            // Dapper ejecuta la sentencia una vez por cada elemento de messagesList (batch) y
            // devuelve la suma de filas afectadas.
            return await _session.Connection.ExecuteAsync(command);
        }
    }
}
