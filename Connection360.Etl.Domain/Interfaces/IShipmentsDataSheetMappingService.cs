using Connection360.Etl.Domain.Entities;
using System.Collections.Generic;

namespace Connection360.Etl.Domain.Interfaces
{
    /// <summary>
    /// Puerto de dominio para el paso "Transform": convierte el <see cref="DynamicDataSet"/>
    /// unificado (BPMS+SIM+OPENCOMEX+ASISCOMEX+SYSTEMCARRIER, ya combinado por
    /// <see cref="IDynamicDataSetMerger"/>) en filas listas para cargar en la bodega de datos. El
    /// histórico de estados (DATALOGS) NO se combina aquí: tiene su propio proceso ETL
    /// independiente hacia connection360write.log_status_tracking, ver
    /// <see cref="ILogStatusMappingService"/>.
    /// </summary>
    public interface IShipmentsDataSheetMappingService
    {
        /// <summary>
        /// </summary>
        /// <param name="unifiedDataSet">Resultado de <see cref="IDynamicDataSetMerger.Merge"/> sobre los datasets operativos.</param>
        /// <returns>Una fila por documento de transporte (HBL) presente en <paramref name="unifiedDataSet"/>.</returns>
        IReadOnlyList<ApplicationDataSheet> Map(DynamicDataSet unifiedDataSet);
    }
}
