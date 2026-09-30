using Connection360.Etl.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Connection360.Etl.Domain.Ports.Persistence
{
    /// <summary>
    /// Puerto secundario (saliente): persiste eventos en la tabla PostgreSQL
    /// connection360write.outbox_messages (patrón Outbox), consumida por el worker existente
    /// Connection360.Infrastructure.Messaging.OutboxPublisherWorker (proceso de la API principal,
    /// ensamblado distinto y no referenciado desde aquí) que republica cada fila pendiente a Kafka.
    /// Lo implementa Connection360.Etl.Infrastructure.
    /// </summary>
    public interface IOutboxMessageRepository
    {
        /// <summary>
        /// Inserta por lote. Igual que <see cref="ILogStatusTrackingRepository.InsertBatchAsync"/>, es
        /// un INSERT simple (sin upsert): cada fila es un evento de dominio independiente, nunca se
        /// actualiza una vez creada (solo el worker de publicación completa "processed_at" después).
        /// </summary>
        /// <returns>Cantidad de filas insertadas.</returns>
        Task<Int32> InsertBatchAsync(IEnumerable<OutboxMessage> messages, CancellationToken cancellationToken = default);
    }
}
