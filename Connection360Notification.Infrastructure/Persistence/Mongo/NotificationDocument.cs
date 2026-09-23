using Connection360Notification.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Connection360Notification.Infrastructure.Persistence.Mongo
{
    /// <summary>
    /// Modelo de persistencia específico de MongoDB, separado a propósito de la entidad de dominio
    /// NotificationMessage: así Domain no depende de MongoDB.Bson ni de ningún paquete de
    /// infraestructura, y el mapeo entre ambos (NotificationDocumentMapper) es el único lugar que
    /// conoce a la vez el modelo de dominio y el de persistencia. Un futuro adaptador para otra base
    /// NoSQL (p.ej. DynamoDB) definiría su propio modelo de persistencia sin tocar este archivo.
    /// </summary>
    public sealed class NotificationDocument
    {
        /// <summary>
        /// Mismo Id (GUID string) que usa la entidad de dominio, reutilizado como "_id" de Mongo y
        /// como Key del mensaje de Kafka. Se usa [BsonId] sin forzar BsonType.ObjectId porque no es
        /// un ObjectId de 24 caracteres hexadecimales: es un GUID, y se guarda tal cual como string.
        /// </summary>
        [BsonId]
        public String Id { get; set; } = default!;

        public Int64 IdNotification { get; set; }

        public String ClientId { get; set; } = default!;

        [BsonRepresentation(BsonType.String)]
        public NotificationType Type { get; set; }

        public String DocumentNumber { get; set; } = String.Empty;

        public String Title { get; set; } = String.Empty;

        public String Message { get; set; } = default!;

        public DateTime MessageDate { get; set; }

        [BsonRepresentation(BsonType.String)]
        public NotificationStatus Status { get; set; }

        public DateTime NotificationDate { get; set; }
    }
}
