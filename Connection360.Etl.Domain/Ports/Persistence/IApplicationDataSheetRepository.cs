using Connection360.Etl.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Connection360.Etl.Domain.Ports.Persistence
{
    /// <summary>
    /// Puerto secundario (saliente) del paso "Load": persiste las filas transformadas en la tabla
    /// PostgreSQL connection360write.application_data_sheet. Lo implementa Connection360.Etl.Infrastructure.
    /// </summary>
    public interface IApplicationDataSheetRepository
    {
        /// <summary>
        /// Inserta o actualiza (upsert) por lote, usando <see cref="ApplicationDataSheet.DocumentoTransporteHbl"/>
        /// como llave de negocio (columna UNIQUE en la tabla), para que reprocesar el mismo período
        /// sea idempotente.
        /// </summary>
        /// <returns>Cantidad de filas afectadas.</returns>
        Task<Int32> UpsertBatchAsync(IEnumerable<ApplicationDataSheet> rows, CancellationToken cancellationToken = default);
    }
}
