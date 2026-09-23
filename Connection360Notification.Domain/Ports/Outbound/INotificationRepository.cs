namespace Connection360Notification.Domain.Ports.Outbound
{
    /// <summary>
    /// Puerto de salida (hexagonal) para la persistencia de notificaciones. El dominio y la
    /// aplicación solo conocen este contrato: la implementación concreta (MongoDB hoy, DynamoDB
    /// u otro motor NoSQL en el futuro) vive en Infrastructure y es intercambiable sin tocar
    /// Domain ni Application.
    /// </summary>
    public interface INotificationRepository
    {
        /// <summary>
        /// Persiste una notificación nueva. Si aún no tiene IdNotification asignado (negocio),
        /// el repositorio se lo asigna antes de guardar (ver NotificationMessage.AssignSequentialId).
        /// </summary>
        Task SaveAsync(NotificationMessage notification, CancellationToken cancellationToken);

        Task<IReadOnlyList<NotificationMessage>> GetAllAsync(CancellationToken cancellationToken);

        /// <summary>Lista las notificaciones de un cliente, más recientes primero.</summary>
        Task<IReadOnlyList<NotificationMessage>> GetByClientAsync(String clientId, CancellationToken cancellationToken);

        Task<NotificationMessage?> GetByIdAsync(String clientId, Int64 idNotification, CancellationToken cancellationToken);

        /// <summary>Marca como leída la notificación indicada. Devuelve false si no existe para ese cliente.</summary>
        Task<Boolean> MarkAsReadAsync(String clientId, Int64 idNotification, CancellationToken cancellationToken);
    }
}
