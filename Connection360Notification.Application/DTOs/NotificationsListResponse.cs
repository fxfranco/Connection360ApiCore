using Connection360Notification.Application.Enum;

namespace Connection360Notification.Application.DTOs
{
    /// <summary>
    /// Representa una notificación en las respuestas de consulta expuestas por la API.
    /// </summary>
    public class NotificationsListResponse
    {
        /// <summary>Identificador técnico de la notificación (clave de almacenamiento/partición de Kafka).</summary>
        public String Id { get; set; } = String.Empty;

        /// <summary>Identificador secuencial de negocio de la notificación, asignado por el repositorio al guardarla.</summary>
        public Int64 IdNotification { get; set; }

        /// <summary>Identificador del cliente destinatario de la notificación.</summary>
        public String ClientId { get; set; } = String.Empty;

        /// <summary>Tipo de evento que originó la notificación.</summary>
        public NotificationType NotificationType { get; set; }

        /// <summary>Número de documento (envío/pedido, etc.) asociado a la notificación, cuando aplica.</summary>
        public String DocumentNumber { get; set; } = String.Empty;

        /// <summary>Título breve de la notificación.</summary>
        public String Title { get; set; } = String.Empty;

        /// <summary>Contenido/mensaje detallado de la notificación.</summary>
        public String Message { get; set; } = String.Empty;        

        /// <summary>Fecha y hora del evento de negocio que originó la notificación.</summary>
        public DateTime MessageDate { get; set; }

        /// <summary>Estado de lectura de la notificación (leída/no leída).</summary>
        public NotificationStatus NotificationStatus { get; set; }        

        /// <summary>Fecha y hora en que la notificación fue registrada/generada.</summary>
        public DateTime NotificationDate {  get; set; }
    }
}
