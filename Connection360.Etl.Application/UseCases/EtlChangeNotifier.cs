using Connection360.Etl.Application.DTOs;
using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Domain.Enums;
using Connection360.Etl.Domain.Interfaces;
using Connection360.Etl.Domain.Ports.Persistence;
using Connection360.Etl.Domain.Time;
using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <summary>Longitud máxima de log_status_tracking.usuario_cambio (VARCHAR(50)).</summary>
        private const Int32 MaxChangeUserLength = 50;

        private readonly ILogStatusTrackingRepository _logRepository;
        private readonly IOutboxMessageRepository _outboxRepository;
        private readonly ICustomerNotificationChannelsRepository _channelsRepository;
        private readonly IEtlChangeMessageCatalog _messageCatalog;
        private readonly String _systemUser;

        public EtlChangeNotifier(
            ILogStatusTrackingRepository logRepository,
            IOutboxMessageRepository outboxRepository,
            ICustomerNotificationChannelsRepository channelsRepository,
            IEtlChangeMessageCatalog messageCatalog,
            String systemUser)
        {
            _logRepository = logRepository;
            _outboxRepository = outboxRepository;
            _channelsRepository = channelsRepository;
            _messageCatalog = messageCatalog;
            _systemUser = systemUser;
        }

        public async Task NotifyChangesAsync(IReadOnlyList<ApplicationDataSheetChange> changes, CancellationToken cancellationToken = default)
        {
            if (changes is null || changes.Count == 0)
                return;

            // Hora de Colombia (UTC-5): es la que quedan en log_status_tracking.fecha_cambio, en el
            // payload (MessageDate) y en outbox_messages.created_at. Se usa UN solo instante por ronda.
            DateTime now = ColombiaClock.Now;

            // Canales de notificación de TODOS los clientes de la ronda en una sola consulta (no una
            // por cambio), dentro de la misma transacción. Un cliente sin configuración (o que no
            // aparece) usa CustomerNotificationChannelSettings.None = todos los canales en false.
            List<String> clientIds = changes.Where(c => c.StateChanged || c.CommentChanged || c.OtherInfoChanged).Select(c => c.NitCliente).Distinct(StringComparer.Ordinal).ToList();
            IReadOnlyDictionary<String, CustomerNotificationChannelSettings> channelsByClient = clientIds.Count == 0
                ? new Dictionary<String, CustomerNotificationChannelSettings>(StringComparer.Ordinal)
                : await _channelsRepository.GetByCustomerIdentificationsAsync(clientIds, cancellationToken);

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
                        UsuarioCambio = ResolveChangeUser(change),
                        Mensaje = message,
                        EstadoAnterior = change.EstadoAnterior,
                        NuevoEstado = change.NuevoEstado,
                    });

                    outboxMessages.Add(BuildOutboxMessage(change, EtlChangeEventType.ChangeState, title, message, now, channelsByClient));
                }

                // Cambio de COMENTARIO y/o FECHA COMENTARIO: únicamente notificación (outbox), sin
                // fila en log_status_tracking.
                if (change.CommentChanged)
                {
                    var (title, message) = _messageCatalog.GetCommentChangeMessage(change.Comentario);
                    outboxMessages.Add(BuildOutboxMessage(change, EtlChangeEventType.Comment, title, message, now, channelsByClient));
                }

                // Cambio de OTRA INFORMACIÓN (transportista, anticipo, fechas logísticas, días libres,
                // demoras...): igual que el comentario, únicamente notificación (outbox). Un mismo
                // documento puede generar a la vez los tres mensajes (estado, comentario y otra
                // información); cada uno se registra por separado.
                if (change.OtherInfoChanged)
                {
                    var (title, message) = _messageCatalog.GetOtherInfoChangeMessage();
                    outboxMessages.Add(BuildOutboxMessage(change, EtlChangeEventType.OtherInfo, title, message, now, channelsByClient));
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
            ApplicationDataSheetChange change, EtlChangeEventType eventType, String title, String message, DateTime now,
            IReadOnlyDictionary<String, CustomerNotificationChannelSettings> channelsByClient)
        {
            CustomerNotificationChannelSettings channels = channelsByClient.TryGetValue(change.NitCliente, out var found)
                ? found
                : CustomerNotificationChannelSettings.None;

            String eventTypeValue = eventType.ToDbValue();

            var payload = new EtlOutboxPayload
            {
                ClientId = change.NitCliente,
                EventType = eventTypeValue,
                DocumentNumber = change.DocumentoTransporteHbl,
                Title = title,
                Message = message,
                MessageDate = now,
                Origen = change.Origen,
                Destino = change.Destino,
                Comentario = change.Comentario,
                FechaComentario = change.FechaComentario,
                EstadoAnterior = change.EstadoAnterior,
                NuevoEstado = change.NuevoEstado,
                NotificationChannel = new EtlNotificationChannelPayload
                {
                    Application = channels.Application,
                    Email = channels.Email,
                    TextMessages = channels.TextMessages,
                },
            };

            return new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = eventTypeValue,
                Payload = JsonSerializer.Serialize(payload),
                // outbox_messages.created_at es timestamptz: Npgsql solo acepta Kind=Utc, así que la hora
                // de Colombia se marca como Utc para que no se vuelva a desplazar al guardarla.
                CreatedAt = DateTime.SpecifyKind(now, DateTimeKind.Utc),
            };
        }

        /// <summary>
        /// Usuario que se registra en log_status_tracking.usuario_cambio: el "changeuser" que devolvieron
        /// las APIs externas si trae datos; si viene vacío, el usuario de sistema configurado en appsettings.
        /// La columna es VARCHAR(50), por lo que el valor se recorta a esa longitud.
        /// </summary>
        private String ResolveChangeUser(ApplicationDataSheetChange change)
        {
            String changeUser = change.ChangeUser?.Trim() ?? String.Empty;

            if (changeUser.Length == 0)
                return _systemUser;

            return changeUser.Length > MaxChangeUserLength ? changeUser.Substring(0, MaxChangeUserLength) : changeUser;
        }
    }
}
