using MongoDB.Bson.Serialization.Attributes;

namespace Connection360Notification.Infrastructure.Persistence.Mongo
{
    /// <summary>
    /// Modelo de persistencia (MongoDB) de la colección <c>basicUserData</c>, separado de la entidad de dominio
    /// <see cref="Connection360Notification.Domain.BasicUserData"/> por el mismo motivo que NotificationDocument.
    /// </summary>
    public sealed class BasicUserDataDocument
    {
        /// <summary>
        /// Id del usuario en Auth0 (por ejemplo <c>auth0|abc123</c>), usado como "_id" de Mongo: al ser la llave primaria
        /// de la colección, MongoDB impide por sí mismo que exista dos veces el mismo usuario.
        /// </summary>
        [BsonId]
        public String Id { get; set; } = default!;

        public String Document { get; set; } = String.Empty;

        public String FullName { get; set; } = String.Empty;

        public String Phone { get; set; } = String.Empty;

        public String Email { get; set; } = String.Empty;

        /// <summary>
        /// Nombre del rol del usuario en Auth0. Los documentos guardados antes de este campo no lo tienen: al leerlos
        /// queda vacío hasta la siguiente sincronización.
        /// </summary>
        public String RoleName { get; set; } = String.Empty;

        /// <summary>Identificador (GUID, como texto) de la sincronización que escribió por última vez este registro.</summary>
        public String SyncId { get; set; } = String.Empty;

        /// <summary>Fecha (UTC) de la última vez que se escribió este registro.</summary>
        public DateTime UpdatedAtUtc { get; set; }
    }
}
