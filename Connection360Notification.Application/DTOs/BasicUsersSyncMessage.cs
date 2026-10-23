namespace Connection360Notification.Application.DTOs
{
    /// <summary>
    /// Mensaje de Kafka con un lote de usuarios de Auth0 que publica Connection360.Api (Auth0UserSyncWorker).
    /// Una sincronización completa llega repartida en varios mensajes con el mismo <see cref="SyncId"/>.
    /// </summary>
    public class BasicUsersSyncMessage
    {
        /// <summary>Identificador de la sincronización a la que pertenece el lote.</summary>
        public Guid SyncId { get; set; }

        /// <summary>Posición del lote dentro de la sincronización (base 0).</summary>
        public Int32 BatchIndex { get; set; }

        /// <summary>Cantidad total de lotes de la sincronización.</summary>
        public Int32 BatchCount { get; set; }

        /// <summary>Cantidad total de usuarios de la sincronización (suma de todos los lotes).</summary>
        public Int32 TotalUsers { get; set; }

        /// <summary>Fecha (UTC) en que Connection360.Api generó la sincronización.</summary>
        public DateTime GeneratedAtUtc { get; set; }

        /// <summary>Usuarios de este lote.</summary>
        public List<BasicUserItem> Users { get; set; } = new();
    }

    /// <summary>Usuario dentro de un <see cref="BasicUsersSyncMessage"/>.</summary>
    public class BasicUserItem
    {
        public String Id { get; set; } = String.Empty;

        public String Document { get; set; } = String.Empty;

        public String FullName { get; set; } = String.Empty;

        public String Phone { get; set; } = String.Empty;

        public String Email { get; set; } = String.Empty;
    }
}
