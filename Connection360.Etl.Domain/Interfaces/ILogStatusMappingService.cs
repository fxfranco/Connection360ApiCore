using Connection360.Etl.Domain.Entities;
using System.Collections.Generic;

namespace Connection360.Etl.Domain.Interfaces
{
    /// <summary>
    /// Puerto de dominio para el paso "Transform" del proceso ETL de logs: convierte el
    /// <see cref="DynamicDataSet"/> devuelto por la API "DATALOGS" en filas listas para cargar en
    /// connection360write.log_status_tracking. Proceso independiente del de la bodega de datos de
    /// envíos (ver <see cref="IShipmentsDataSheetMappingService"/>).
    /// </summary>
    public interface ILogStatusMappingService
    {
        /// <param name="dataLogsDataSet">Una página (o, sin paginación, el 100% de los datos) de <c>IExternalDataGateway.FetchDataPagedAsync("DATALOGS", ...)</c>, sin combinar con ningún otro dataset.</param>
        /// <returns>Una fila por registro de log presente en <paramref name="dataLogsDataSet"/> que tenga documento de transporte.</returns>
        IReadOnlyList<LogStatusTracking> Map(DynamicDataSet dataLogsDataSet);
    }
}
