using Connection360.Etl.Application.DTOs;
using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Domain.Enums;
using Connection360.Etl.Domain.Interfaces;
using Connection360.Etl.Domain.Ports.Persistence;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Connection360.Etl.Application.UseCases
{
    /// <summary>
    /// Envuelve <see cref="ILogStatusTrackingRepository"/> y <see cref="IOutboxMessageRepository"/>
    /// para que <see cref="RunEtlProcessUseCase"/> no repita la mecánica de "armar las filas de log y
    /// de outbox a partir de los cambios detectados". Mismo patrón que <see cref="EtlJobControlTracker"/>:
    /// se construye UNA vez (sobre los repositorios ya resueltos del mismo <c>IUnitOfWork</c>/DbSession)
    /// y <see cref="NotifyChangesAsync"/> se llama una vez por ronda, siempre DENTRO de la transacción
    /// activa de esa ronda (la misma que ya usa el Load principal), para que la integridad entre
    /// application_data_sheet, log_status_tracking y outbox_messages quede garantizada por una única
    /// transacción: si algo aquí falla, <see cref="EtlTransactionHelper.RunInOwnTransactionAsync"/>
    /// revierte también el upsert y el avance de etl_job_control de esa misma ronda.
    /// </summary>
    internal sealed class EtlChangeNotifier
    {
        private readonly ILogStatusTrackingRepository _logRepository;
        private readonly IOutboxMessageRepository _outboxRepository;
        private readonly IEtlChangeMessageCatalog _messageCatalog;
        private readonly String _systemUser;

        public EtlChangeNotifier(
            ILogStatusTrackingRepository logRepository,
            IOutboxMessageRepository outboxRepository,
            IEtlChangeMessageCatalog messageCatalog,
            String systemUser)
        {
            _logRepository = logRepository;
            _outboxRepository = outboxRepository;
            _messageCatalog = messageCatalog;
            _systemUser = systemUser;
        }

        public async Task NotifyChangesAsync(IReadOnlyList<ApplicationDataSheetChange> changes, CancellationToken cancellationToken = default)
        {
            if (changes is null || changes.Count == 0)
                return;

            DateTime now = DateTime.UtcNow;

            var logRows = new List<LogStatusTracking>();
            var outboxMessages = new List<OutboxMessage>();

            foreach (var change in changes)
            {
                // Cambio de ESTADO: además de la notificación (outbox), queda registrado en el
                // histórico de trazabilidad/auditoría (log_status_tracking).
                if (change.StateChanged)
                {
                    var (title, message) = _messageCatalog.GetStateChangeMessage(change.NuevoEstado);

                    logRows.Add(new LogStatusTracking
                    {
                        IdOperacion = change.IdOperacion,
                        DocumentoTransporteHbl = change.DocumentoTransporteHbl,
                        FechaCambio = now,
                        UsuarioCambio = _systemUser,
                        Mensaje = message,
                        EstadoAnterior = change.EstadoAnterior,
                        NuevoEstado = change.NuevoEstado,
                    });

                    outboxMessages.Add(BuildOutboxMessage(change, EtlChangeEventType.ChangeState, title, message, now));
                }

                // Cambio de COMENTARIO y/o FECHA COMENTARIO: únicamente notificación (outbox), sin
                // fila en log_status_tracking.
                if (change.CommentChanged)
                {
                    var (title, message) = _messageCatalog.GetCommentChangeMessage();
                    outboxMessages.Add(BuildOutboxMessage(change, EtlChangeEventType.Comment, title, message, now));
                }
            }

            // Dos INSERT por lote (uno por tabla) en vez de uno por fila: mismo patrón "batch" que ya
            // usan UpsertBatchAsync/InsertBatchAsync para minimizar el número de round-trips.
            if (logRows.Count > 0)
                await _logRepository.InsertBatchAsync(logRows, cancellationToken);

            if (outboxMessages.Count > 0)
                await _outboxRepository.InsertBatchAsync(outboxMessages, cancellationToken);
        }

        private static OutboxMessage BuildOutboxMessage(
            ApplicationDataSheetChange change, EtlChangeEventType eventType, String title, String message, DateTime now)
        {
            String eventTypeValue = eventType.ToDbValue();

            var payload = new EtlOutboxPayload
            {
                ClientId = change.NitCliente,
                EventType = eventTypeValue,
                DocumentNumber = change.DocumentoTransporteHbl,
                Title = title,
                Message = message,
                MessageDate = now,
            };

            return new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = eventTypeValue,
                Payload = JsonSerializer.Serialize(payload),
                CreatedAt = now,
            };
        }
    }
}
