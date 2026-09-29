using Connection360.Etl.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Connection360.Etl.Domain.Ports.Persistence
{
    /// <summary>
    /// Puerto secundario (saliente) para connection360write.etl_job_control (ver
    /// Documents/scriptetlJobControlSQL.sql). Lo implementa Connection360.Etl.Infrastructure.
    /// </summary>
    public interface IEtlJobControlRepository
    {
        /// <summary>True si ya existe al menos un registro de <paramref name="jobName"/> en estado COMPLETED.</summary>
        Task<Boolean> HasCompletedRunAsync(EtlJobName jobName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserta un nuevo registro en estado PROCESSING (updated_at = ahora, total_records_processed = 0)
        /// y devuelve su Id. Se ejecuta en autocommit (sin transacción activa), a propósito: debe quedar
        /// registrado en la tabla de control aunque la corrida falle más adelante, para poder marcarlo
        /// como FAILED y auditar en qué página quedó.
        /// </summary>
        Task<Int64> StartRunAsync(EtlJobName jobName, Int32? pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Suma <paramref name="recordsProcessedInPage"/> a total_records_processed y actualiza
        /// last_processed_page y updated_at. Debe llamarse DENTRO de la misma transacción que la
        /// carga de esa página/ronda, para que ambos cambios se confirmen o se reviertan juntos.
        /// </summary>
        Task RegisterPageProgressAsync(Int64 jobControlId, Int32 lastProcessedPage, Int32 recordsProcessedInPage, CancellationToken cancellationToken = default);

        /// <summary>Marca el registro como COMPLETED y actualiza updated_at.</summary>
        Task CompleteRunAsync(Int64 jobControlId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marca el registro como FAILED y actualiza updated_at. Se ejecuta en autocommit: se llama
        /// DESPUÉS de que la transacción de la página que falló ya se revirtió (si fuera parte de esa
        /// misma transacción, el rollback también revertiría este cambio).
        /// </summary>
        Task FailRunAsync(Int64 jobControlId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina los registros de <paramref name="jobName"/> cuyo updated_at sea anterior a hoy
        /// menos <paramref name="olderThanDays"/> días. Lo usa el proceso de depuración
        /// (PurgeEtlJobControlUseCase), que solo debe invocarse para EtlJobName.ApplicationDataSheet:
        /// los registros de LogStatusTracking nunca se depuran.
        /// </summary>
        /// <returns>Cantidad de registros eliminados.</returns>
        Task<Int32> DeleteOlderThanAsync(EtlJobName jobName, Int32 olderThanDays, CancellationToken cancellationToken = default);
    }
}
