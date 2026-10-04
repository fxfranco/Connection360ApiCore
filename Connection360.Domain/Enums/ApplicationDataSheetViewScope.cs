namespace Connection360.Domain.Enums
{
    /// <summary>
    /// Alcance de consulta sobre las vistas de connection360write.application_data_sheet, usado por
    /// Connection360.Application.Services.ApplicationDataSheetDataGateway para decidir cuál(es)
    /// repositorio(s) consultar: IApplicationDataSheetEntregadosRepository,
    /// IApplicationDataSheetNoEntregadosRepository, o ambos.
    /// </summary>
    public enum ApplicationDataSheetViewScope
    {
        /// <summary>Solo connection360read.vw_application_data_sheet_entregados (estado = 'Entregado').</summary>
        Entregados,

        /// <summary>Solo connection360read.vw_application_data_sheet_no_entregados (estado distinto de 'Entregado').</summary>
        NoEntregados,

        /// <summary>Ambas vistas combinadas: el mismo universo de filas que connection360write.application_data_sheet.</summary>
        Todos
    }
}
