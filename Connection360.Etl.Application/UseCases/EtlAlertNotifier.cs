using Connection360.Etl.Application.DTOs;
using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Domain.Enums;
using Connection360.Etl.Domain.Interfaces;
using Connection360.Etl.Domain.Ports.Persistence;
using Connection360.Etl.Domain.Time;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Connection360.Etl.Application.UseCases
{
    /// <summary>
    /// Registra en connection360write.outbox_messages la alerta que avisa a los administradores de que un error impidió
    /// consultar las APIs externas del proceso ETL. Mismo patrón que <see cref="EtlChangeNotifier"/> (arma el
    /// <see cref="OutboxMessage"/> y lo inserta con <see cref="IOutboxMessageRepository"/>; el publicador de la API
    /// principal lo envía tal cual a Kafka), pero para un evento del proceso y no de un documento:
    /// <list type="bullet">
    /// <item><description>EventType "Alert" (<see cref="EtlChangeEventType.Alert"/>), título y mensaje del catálogo
    /// (<see cref="IEtlChangeMessageCatalog.GetEtlAlertMessage"/>) con el error sanitizado (<see cref="EtlErrorSanitizer"/>).</description></item>
    /// <item><description>Del payload solo se llenan Title, Message y MessageDate; el resto de campos conserva su valor por defecto (cadena
    /// vacía / fecha por defecto) para que el JSON siga teniendo todos los campos que esperan los consumidores.</description></item>
    /// <item><description>NotificationChannel con Application, Email y TextMessages en true: una alerta siempre se notifica por todos los canales.</description></item>
    /// </list>
    /// El destinatario lo resuelve el servicio de notificaciones (usuarios con rol ADMIN); aquí no hay ClientId.
    /// </summary>
    public sealed class EtlAlertNotifier
    {
        private readonly IOutboxMessageRepository _outboxRepository;
        private readonly IEtlChangeMessageCatalog _messageCatalog;

        public EtlAlertNotifier(IOutboxMessageRepository outboxRepository, IEtlChangeMessageCatalog messageCatalog)
        {
            _outboxRepository = outboxRepository;
            _messageCatalog = messageCatalog;
        }

        /// <summary>
        /// Inserta la alerta de un fallo de extracción. Devuelve el mensaje de outbox insertado.
        /// </summary>
        /// <param name="error">Error capturado al consultar las APIs externas (se sanitiza antes de usarlo).</param>
        public async Task<OutboxMessage> NotifyExtractionFailureAsync(Exception error, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(error);

            var (title, message) = _messageCatalog.GetEtlAlertMessage(EtlErrorSanitizer.Sanitize(error));
            OutboxMessage outboxMessage = BuildOutboxMessage(title, message, ColombiaClock.Now);

            await _outboxRepository.InsertBatchAsync(new[] { outboxMessage }, cancellationToken);
            return outboxMessage;
        }

        public static OutboxMessage BuildOutboxMessage(String title, String message, DateTime now)
        {
            String eventTypeValue = EtlChangeEventType.Alert.ToDbValue();

            var payload = new EtlOutboxPayload
            {
                EventType = eventTypeValue,
                Title = title ?? String.Empty,
                Message = message ?? String.Empty,
                MessageDate = now,
                NotificationChannel = new EtlNotificationChannelPayload
                {
                    Application = true,
                    Email = true,
                    TextMessages = true,
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
    }
}
