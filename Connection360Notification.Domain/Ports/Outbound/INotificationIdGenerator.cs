namespace Connection360Notification.Domain.Ports.Outbound
{
    /// <summary>
    /// Puerto de salida para generar identificadores secuenciales de negocio (IdNotification),
    /// desacoplado del motor de persistencia concreto. La implementación de MongoDB usa un
    /// contador atómico propio de Mongo; un futuro adaptador DynamoDB podría implementarlo con
    /// su propio mecanismo (p.ej. un ítem contador con actualización atómica ADD), sin que
    /// Domain ni Application necesiten cambiar.
    /// </summary>
    public interface INotificationIdGenerator
    {
        Task<Int64> NextIdAsync(CancellationToken cancellationToken);
    }
}
