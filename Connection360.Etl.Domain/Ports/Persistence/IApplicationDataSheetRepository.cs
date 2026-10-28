using Connection360.Etl.Domain.Entities;
using System;
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

        /// <summary>
        /// Devuelve el snapshot "antes" (Estado/Comentario/FechaComentario tal como están HOY en la
        /// tabla) de únicamente los documentos de transporte indicados, para detección de cambios
        /// (ver <see cref="Interfaces.IApplicationDataSheetChangeDetector"/>). Debe llamarse ANTES de
        /// <see cref="UpsertBatchAsync"/> en la misma ronda/página (y, para que la comparación sea
        /// consistente, dentro de la misma transacción), ya que el upsert sobreescribe esas mismas
        /// columnas. Solo trae esas 4 columnas (no la fila completa) y solo para los documentos
        /// pedidos, para minimizar el impacto en rendimiento y transferencia de datos.
        /// </summary>
        /// <returns>
        /// Snapshot por documento de transporte. Un documento sin fila previa en la tabla (aún no
        /// existía) simplemente no aparece en el resultado.
        /// </returns>
        Task<IReadOnlyDictionary<String, ApplicationDataSheetChangeSnapshot>> GetChangeSnapshotsAsync(
            IEnumerable<String> documentNumbers, CancellationToken cancellationToken = default);

        /// <summary>
        /// Devuelve el Id (BIGSERIAL) de application_data_sheet para cada uno de los documentos de
        /// transporte indicados, tal como quedó DESPUÉS del upsert más reciente en esa misma
        /// transacción. Se usa únicamente para completar
        /// <see cref="Entities.ApplicationDataSheetChange.IdOperacion"/> de los documentos NUEVOS de
        /// la ronda (<see cref="Entities.ApplicationDataSheetChange.IsNewDocument"/> true): para
        /// esos, el Id recién se generó en el upsert de esa misma ronda, así que no puede venir del
        /// snapshot "antes" de <see cref="GetChangeSnapshotsAsync"/> (que se consulta ANTES del
        /// upsert, cuando el documento todavía no existía). Debe llamarse DESPUÉS del upsert.
        /// </summary>
        Task<IReadOnlyDictionary<String, Int64>> GetIdsByDocumentAsync(
            IEnumerable<String> documentNumbers, CancellationToken cancellationToken = default);
    }
}
