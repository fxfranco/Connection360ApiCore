namespace Connection360Notification.Application.DTOs
{
    /// <summary>Resultado de procesar un lote de usuarios (para que quien lo invoca pueda registrarlo).</summary>
    /// <param name="Upserted">Usuarios válidos que se guardaron (insertados o actualizados) en este lote.</param>
    /// <param name="Discarded">Usuarios descartados por no traer Id.</param>
    /// <param name="IsLastBatch">Si el lote era el último de la sincronización.</param>
    /// <param name="CleanupPerformed">Si, por ser el último lote y estar completa la sincronización, se limpiaron los usuarios obsoletos.</param>
    /// <param name="Deleted">Usuarios obsoletos (ya no existen en Auth0) que se eliminaron.</param>
    /// <param name="StoredInSync">En el último lote: cuántos registros llevaba guardados la sincronización al verificar que estuviera completa.</param>
    public sealed record BasicUsersSyncResult(Int32 Upserted, Int32 Discarded, Boolean IsLastBatch, Boolean CleanupPerformed, Int64 Deleted, Int64 StoredInSync);
}
