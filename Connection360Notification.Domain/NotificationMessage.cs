using Connection360Notification.Domain.Enums;
using System.Text.Json.Serialization;

namespace Connection360Notification.Domain
{
    /// <summary>
    /// Entidad de dominio de una notificación. Su forma está alineada a
    /// Connection360Notification.Application.DTOs.NotificationsListResponse (más ClientId, que la
    /// respuesta de listado no necesita porque el filtrado por cliente ya se aplicó del lado del
    /// servidor) para que el repositorio pueda almacenar y listar exactamente esa estructura, con
    /// un mapeo directo a NotificationsListResponse en Application.Mapping.NotificationMappingExtensions.
    ///
    /// No depende de ningún paquete de infraestructura (MongoDB, DynamoDB, etc.). Id es un
    /// identificador técnico propio del dominio (no un ObjectId de Mongo): se genera aquí mismo al
    /// construirse y sirve tanto de clave de partición en Kafka como de "_id" al persistir, sin que
    /// el dominio necesite saber nada del motor de almacenamiento. IdNotification, en cambio, es un
    /// identificador secuencial de negocio que solo el repositorio puede asignar (ver
    /// AssignSequentialId), porque solo él sabe cómo generarlo de forma consistente en su motor.
    /// </summary>
    public sealed class NotificationMessage
    {
        /// <summary>Identificador técnico de la notificación (clave de almacenamiento y de partición en Kafka). No es un ObjectId de Mongo.</summary>
        public String Id { get; private set; }

        /// <summary>Identificador secuencial de negocio, asignado por el repositorio al guardar (ver <see cref="AssignSequentialId"/>).</summary>
        public Int64 IdNotification { get; private set; }

        /// <summary>Identificador del cliente destinatario de la notificación.</summary>
        public String ClientId { get; private set; }

        /// <summary>Tipo de evento que originó la notificación.</summary>
        public NotificationType Type { get; private set; }

        /// <summary>Número de documento (envío/pedido, etc.) asociado a la notificación, cuando aplica.</summary>
        public String DocumentNumber { get; private set; }

        /// <summary>Título breve de la notificación.</summary>
        public String Title { get; private set; }

        /// <summary>Contenido/mensaje detallado de la notificación.</summary>
        public String Message { get; private set; }

        /// <summary>Fecha y hora del evento de negocio que originó la notificación.</summary>
        public DateTime MessageDate { get; private set; }

        /// <summary>Estado de lectura de la notificación (ver <see cref="MarkAsRead"/>).</summary>
        public NotificationStatus Status { get; private set; }

        /// <summary>Fecha y hora en que la notificación fue registrada/generada.</summary>
        public DateTime NotificationDate { get; private set; }

        // IMPORTANTE: messageDate y notificationDate son "DateTime" (no "DateTime?"), a
        // propósito. System.Text.Json solo puede usar un constructor marcado con [JsonConstructor]
        // para deserializar si CADA parámetro coincide en nombre (sin distinguir mayúsculas de
        // minúsculas) Y EN TIPO EXACTO con una propiedad pública. A diferencia de los tipos
        // referencia (donde "String" y "String?" son el mismo tipo en tiempo de ejecución),
        // "DateTime?" es en realidad "Nullable<DateTime>": un tipo distinto de "DateTime" a nivel
        // de runtime. Tener aquí "DateTime? messageDate" mientras la propiedad MessageDate es
        // "DateTime" hacía que el constructor NO calificara como constructor de deserialización
        // válido, y System.Text.Json lanzaba JsonException ("must bind to an object property or
        // field") al intentar deserializar los mensajes que llegan por Kafka.
        [JsonConstructor]
        public NotificationMessage(
            String clientId,
            NotificationType type,
            String message,
            String documentNumber = "",
            String title = "",
            DateTime messageDate = default,
            NotificationStatus status = NotificationStatus.Unread,
            DateTime notificationDate = default,
            Int64 idNotification = 0,
            String? id = null)
        {
            if (String.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("El cliente destino de la notificación es obligatorio.", nameof(clientId));

            if (String.IsNullOrWhiteSpace(message))
                throw new ArgumentException("El mensaje de la notificación es obligatorio.", nameof(message));

            Id = String.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString() : id;
            IdNotification = idNotification;
            ClientId = clientId;
            Type = type;
            Message = message;
            DocumentNumber = documentNumber ?? String.Empty;
            Title = title ?? String.Empty;
            MessageDate = messageDate == default ? DateTime.UtcNow : messageDate;
            Status = status;
            NotificationDate = notificationDate == default ? DateTime.UtcNow : notificationDate;
        }

        /// <summary>Asignado por el repositorio al guardar: identificador secuencial de negocio, consistente con NotificationsListResponse.IdNotification.</summary>
        public void AssignSequentialId(Int64 idNotification) => IdNotification = idNotification;

        /// <summary>Marca la notificación como leída.</summary>
        public void MarkAsRead() => Status = NotificationStatus.Read;
    }
}
