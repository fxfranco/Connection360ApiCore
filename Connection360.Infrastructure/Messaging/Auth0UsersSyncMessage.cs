using Connection360.Domain.Dtos;

namespace Connection360.Infrastructure.Messaging
{
    /// <summary>
    /// Mensaje que <see cref="Auth0UserSyncWorker"/> publica en el tópico de sincronización de usuarios. Una
    /// sincronización completa se divide en varios mensajes (lotes) para no superar el tamaño máximo de un
    /// mensaje de Kafka; todos los lotes de una misma sincronización comparten <see cref="SyncId"/> y se
    /// publican con esa misma clave, de modo que Kafka los entrega en orden al consumidor.
    /// El contrato JSON (camelCase) lo lee Connection360Notification.Api.
    /// </summary>
    public class Auth0UsersSyncMessage
    {
        /// <summary>Identificador de la sincronización a la que pertenece el lote.</summary>
        public Guid SyncId { get; set; }

        /// <summary>Posición del lote dentro de la sincronización (base 0).</summary>
        public Int32 BatchIndex { get; set; }

        /// <summary>Cantidad total de lotes de la sincronización.</summary>
        public Int32 BatchCount { get; set; }

        /// <summary>Cantidad total de usuarios de la sincronización (suma de todos los lotes).</summary>
        public Int32 TotalUsers { get; set; }

        /// <summary>Fecha (UTC) en que se generó la sincronización.</summary>
        public DateTime GeneratedAtUtc { get; set; }

        /// <summary>Usuarios de este lote.</summary>
        public List<Auth0UserBasicDto> Users { get; set; } = new();
    }
}
