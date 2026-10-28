namespace Connection360.Etl.Domain.Enums
{
    /// <summary>
    /// Vistas materializadas del esquema de lectura (connection360read) que la ETL mantiene
    /// sincronizadas con las tablas de escritura (connection360write) una vez termina de cargarlas
    /// (ver Documents/vw_*.sql). Es una lista cerrada a propósito: el adaptador de persistencia
    /// asocia cada valor con su sentencia REFRESH fija, así que ningún nombre de vista llega a SQL
    /// desde configuración o parámetros.
    /// </summary>
    public enum MaterializedViewName
    {
        /// <summary>connection360read.vw_log_status_tracking (origen: log_status_tracking).</summary>
        LogStatusTracking,

        /// <summary>connection360read.vw_application_data_sheet_entregados (origen: application_data_sheet, estado = 'Entregado').</summary>
        ApplicationDataSheetEntregados,

        /// <summary>connection360read.vw_application_data_sheet_no_entregados (origen: application_data_sheet, estado != 'Entregado').</summary>
        ApplicationDataSheetNoEntregados
    }
}
