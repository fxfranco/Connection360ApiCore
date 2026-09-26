namespace Connection360.Application.DTOs
{
    /// <summary>
    /// Eventos de negocio por los que un cliente desea recibir notificación, expuestos dentro de
    /// <see cref="NotificationsSettingsResponse.NotificationEvents"/>.
    /// </summary>
    public class NotificationEventsResponse
    {
        /// <summary>Identificador del cliente dueño de la configuración.</summary>
        public String ClientId { get; set; }

        /// <summary>Identificador del registro de eventos de notificación.</summary>
        public Int64 NotificationEventId { get; set; }

        /// <summary>Notificar ante cambios de estado del envío.</summary>
        public Boolean ChangeState { get; set; }

        /// <summary>Notificar cuando el envío se entrega exitosamente.</summary>
        public Boolean SuccessfulDelivery { get; set; }

        /// <summary>Notificar cuando el envío presenta novedades.</summary>
        public Boolean WithIssues { get; set; }

        /// <summary>Notificar cuando el envío inicia tránsito.</summary>
        public Boolean ShipmentTransit { get; set; }

        /// <summary>Notificar como recordatorio de entrega próxima.</summary>
        public Boolean DeliveryReminder { get; set; }
    }
}
