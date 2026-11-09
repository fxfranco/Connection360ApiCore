using Connection360.Etl.Domain.Enums;
using Connection360.Etl.Domain.Ports.Persistence;
using Connection360.Etl.Infrastructure.Configuration;
using Dapper;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Connection360.Etl.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Adaptador de <see cref="IMaterializedViewRepository"/>: ejecuta REFRESH MATERIALIZED VIEW
    /// CONCURRENTLY sobre las vistas de connection360read (ver Documents/vw_*.sql).
    /// <para>
    /// CONCURRENTLY: PostgreSQL calcula el nuevo contenido aparte y lo aplica como diferencias, de
    /// modo que los SELECT sobre la vista no se bloquean durante el refresco (solo impide otro
    /// refresco simultáneo de la misma vista). Requiere la vista poblada y un índice UNIQUE (los
    /// scripts lo crean: id), y que el usuario de la ETL sea dueño de la vista.
    /// </para>
    /// Las sentencias son constantes fijas por valor del enum: ningún nombre llega desde fuera.
    /// No usa <see cref="DbSession.Transaction"/>: la sesión no tiene transacción abierta cuando
    /// se invoca, así que cada refresco se confirma por sí mismo (autocommit).
    /// </summary>
    public class MaterializedViewRepository : IMaterializedViewRepository
    {
        private readonly DbSession _session;
        private readonly Int32 _commandTimeoutSeconds;

        public MaterializedViewRepository(DbSession session, IOptions<MaterializedViewSettings> settings)
        {
            _session = session;
            _commandTimeoutSeconds = Math.Max(0, settings.Value.CommandTimeoutSeconds);
        }

        public async Task RefreshConcurrentlyAsync(MaterializedViewName view, CancellationToken cancellationToken = default)
        {
            String statement = view switch
            {
                MaterializedViewName.LogStatusTracking =>
                    "REFRESH MATERIALIZED VIEW CONCURRENTLY connection360read.vw_log_status_tracking;",
                MaterializedViewName.ApplicationDataSheetEntregados =>
                    "REFRESH MATERIALIZED VIEW CONCURRENTLY connection360read.vw_application_data_sheet_entregados;",
                MaterializedViewName.ApplicationDataSheetNoEntregados =>
                    "REFRESH MATERIALIZED VIEW CONCURRENTLY connection360read.vw_application_data_sheet_no_entregados;",
                _ => throw new ArgumentOutOfRangeException(nameof(view), view, "MaterializedViewName sin sentencia REFRESH asignada.")
            };

            await _session.EnsureConnectionOpenAsync(cancellationToken);

            var command = new CommandDefinition(
                statement,
                transaction: _session.Transaction,
                commandTimeout: _commandTimeoutSeconds,
                cancellationToken: cancellationToken);

            await _session.Connection.ExecuteAsync(command);
        }
    }
}
