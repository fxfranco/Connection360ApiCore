using Connection360.Etl.Domain.Enums;
using Connection360.Etl.Domain.Ports.Persistence;
using Dapper;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Connection360.Etl.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Adaptador de connection360write.etl_job_control (ver Documents/scriptetlJobControlSQL.sql).
    /// Mismo patrón Dapper + <see cref="DbSession"/> que el resto de repositorios: cada método pasa
    /// <c>transaction: _session.Transaction</c>, que puede ser <c>null</c> (autocommit, para
    /// StartRunAsync/FailRunAsync/DeleteOlderThanAsync) o la transacción activa de la página/ronda en
    /// curso (para RegisterPageProgressAsync/CompleteRunAsync, llamados desde dentro de esa transacción).
    /// </summary>
    public class EtlJobControlRepository : IEtlJobControlRepository
    {
        private readonly DbSession _session;

        public EtlJobControlRepository(DbSession session)
        {
            _session = session;
        }

        public async Task<Boolean> HasCompletedRunAsync(EtlJobName jobName, CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);

            const string query = @"
                SELECT EXISTS (
                    SELECT 1 FROM connection360write.etl_job_control
                    WHERE job_name = @JobName AND status = @Status
                );";

            var command = new CommandDefinition(
                query,
                new { JobName = jobName.ToDbValue(), Status = EtlJobStatus.Completed.ToDbValue() },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken);

            return await _session.Connection.ExecuteScalarAsync<Boolean>(command);
        }

        public async Task<Int64> StartRunAsync(EtlJobName jobName, Int32? pageSize, CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);

            const string query = @"
                INSERT INTO connection360write.etl_job_control
                    (job_name, status, last_processed_page, page_size, total_records_processed, updated_at)
                VALUES
                    (@JobName, @Status, NULL, @PageSize, 0, @UpdatedAt)
                RETURNING id;";

            var command = new CommandDefinition(
                query,
                new
                {
                    JobName = jobName.ToDbValue(),
                    Status = EtlJobStatus.Processing.ToDbValue(),
                    PageSize = pageSize,
                    UpdatedAt = DateTime.UtcNow
                },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken);

            return await _session.Connection.ExecuteScalarAsync<Int64>(command);
        }

        public async Task RegisterPageProgressAsync(Int64 jobControlId, Int32 lastProcessedPage, Int32 recordsProcessedInPage, CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);

            const string query = @"
                UPDATE connection360write.etl_job_control
                SET last_processed_page = @LastProcessedPage,
                    total_records_processed = COALESCE(total_records_processed, 0) + @RecordsProcessedInPage,
                    updated_at = @UpdatedAt
                WHERE id = @Id;";

            var command = new CommandDefinition(
                query,
                new
                {
                    Id = jobControlId,
                    LastProcessedPage = lastProcessedPage,
                    RecordsProcessedInPage = recordsProcessedInPage,
                    UpdatedAt = DateTime.UtcNow
                },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken);

            await _session.Connection.ExecuteAsync(command);
        }

        public async Task CompleteRunAsync(Int64 jobControlId, CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);

            const string query = @"
                UPDATE connection360write.etl_job_control
                SET status = @Status,
                    updated_at = @UpdatedAt
                WHERE id = @Id;";

            var command = new CommandDefinition(
                query,
                new { Id = jobControlId, Status = EtlJobStatus.Completed.ToDbValue(), UpdatedAt = DateTime.UtcNow },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken);

            await _session.Connection.ExecuteAsync(command);
        }

        public async Task FailRunAsync(Int64 jobControlId, CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);

            const string query = @"
                UPDATE connection360write.etl_job_control
                SET status = @Status,
                    updated_at = @UpdatedAt
                WHERE id = @Id;";

            var command = new CommandDefinition(
                query,
                new { Id = jobControlId, Status = EtlJobStatus.Failed.ToDbValue(), UpdatedAt = DateTime.UtcNow },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken);

            await _session.Connection.ExecuteAsync(command);
        }

        public async Task<Int32> DeleteOlderThanAsync(EtlJobName jobName, Int32 olderThanDays, CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);

            const string query = @"
                DELETE FROM connection360write.etl_job_control
                WHERE job_name = @JobName
                  AND updated_at < (CURRENT_TIMESTAMP - (INTERVAL '1 day' * @OlderThanDays));";

            var command = new CommandDefinition(
                query,
                new { JobName = jobName.ToDbValue(), OlderThanDays = olderThanDays },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken);

            return await _session.Connection.ExecuteAsync(command);
        }
    }
}
