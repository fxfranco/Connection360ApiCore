namespace Connection360Notification.Domain.Ports.Outbound
{
    /// <summary>
    /// Puerto de salida de la colección <c>basicUserData</c>. Cada usuario se identifica por su Id de Auth0, así
    /// que escribirlo dos veces actualiza el mismo registro y nunca lo duplica. Cada escritura marca el registro
    /// con el identificador de la sincronización (<c>syncId</c>) que lo trajo: al terminar una sincronización
    /// completa, los registros que conservan una marca anterior son usuarios que ya no existen en Auth0.
    /// </summary>
    public interface IBasicUserDataRepository
    {
        /// <summary>
        /// Inserta o actualiza (upsert por Id) los usuarios y los marca con <paramref name="syncId"/>.
        /// Es idempotente: reenviar el mismo lote deja el mismo resultado.
        /// </summary>
        Task UpsertBatchAsync(IReadOnlyCollection<BasicUserData> users, Guid syncId, CancellationToken cancellationToken);

        /// <summary>
        /// Busca el usuario cuyo <see cref="BasicUserData.Document"/> es igual a <paramref name="document"/> (el
        /// <c>ClientId</c> de una notificación). Devuelve <c>null</c> si el documento está vacío o si no existe ningún
        /// usuario con ese documento; nunca lanza por "no encontrado". Si hubiera varios con el mismo documento
        /// devuelve uno de ellos.
        /// </summary>
        Task<BasicUserData?> GetByDocumentAsync(String document, CancellationToken cancellationToken);

        /// <summary>Cuenta los registros marcados con <paramref name="syncId"/> (los que ya se guardaron en esa sincronización).</summary>
        Task<Int64> CountBySyncAsync(Guid syncId, CancellationToken cancellationToken);

        /// <summary>Elimina los registros que NO pertenecen a <paramref name="syncId"/> y devuelve cuántos eliminó.</summary>
        Task<Int64> DeleteNotInSyncAsync(Guid syncId, CancellationToken cancellationToken);
    }
}
