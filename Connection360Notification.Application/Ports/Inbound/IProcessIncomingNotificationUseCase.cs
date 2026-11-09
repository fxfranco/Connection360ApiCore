using Connection360Notification.Domain;

namespace Connection360Notification.Application.Ports.Inbound
{
    /// <summary>
    /// Puerto de entrada que procesa una notificación ya recibida desde el broker de mensajería
    /// (hoy Kafka, a través de KafkaConsumerHostedService): la persiste y además la envía en
    /// tiempo real al cliente conectado por SignalR. Vive en Application (no en Infrastructure)
    /// para que la orquestación "guardar + notificar" sea una regla de negocio explícita y
    /// testeable, en vez de lógica mezclada directamente en el consumidor de Kafka.
    /// </summary>
    public interface IProcessIncomingNotificationUseCase
    {
        Task ExecuteAsync(NotificationMessage notification, CancellationToken cancellationToken);
    }
}
