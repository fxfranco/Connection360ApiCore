namespace Connection360Notification.Application.DTOs
{
    /// <summary>
    /// Identifica una notificación puntual de un cliente, usado para operaciones como marcarla como leída.
    /// </summary>
    public class NotificationsRequest
    {
        /// <summary>Identificador del cliente propietario de la notificación.</summary>
        public String IdClient { get; set; } = default!;

        /// <summary>Identificador secuencial de negocio de la notificación sobre la que se opera.</summary>
        public Int64 IdNotification { get ; set; }

        /// <summary>Rol del usuario autenticado que realiza la operación.</summary>
        public String RoleName { get; set; } = default!;
    }
}
