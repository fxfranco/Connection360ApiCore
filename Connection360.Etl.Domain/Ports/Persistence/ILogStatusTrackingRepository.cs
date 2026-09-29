using Connection360.Etl.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Connection360.Etl.Domain.Ports.Persistence
{
    /// <summary>
    /// Puerto secundario (saliente) del paso "Load" del proceso ETL de logs: persiste las filas
    /// transformadas en la tabla PostgreSQL connection360write.log_status_tracking. Lo implementa
    /// Connection360.Etl.Infrastructure.
    /// </summary>
    public interface ILogStatusTrackingRepository
    {
        /// <summary>
        /// Inserta por lote. A diferencia de <see cref="IApplicationDataSheetRepository.UpsertBatchAsync"/>,
        /// aquí NO se hace upsert: log_status_tracking no tiene ninguna columna UNIQUE aparte de su
        /// "id" autogenerado (ver Documents/scriptlogsSQL.sql), por lo que cada fila del histórico se
        /// trata como un evento de auditoría independiente (append-only), igual que cualquier tabla de
        /// bitácora/trazabilidad.
        /// </summary>
        /// <returns>Cantidad de filas insertadas.</returns>
        Task<Int32> InsertBatchAsync(IEnumerable<LogStatusTracking> rows, CancellationToken cancellationToken = default);
    }
}
