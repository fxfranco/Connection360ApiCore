using Connection360.Domain.Dtos;
using Connection360.Domain.Ports.Persistence;
using Dapper;

namespace Connection360.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Acceso de solo lectura a connection360read.vw_log_status_tracking (ver Documents/vw_log_status_tracking.sql).
    /// Sigue el mismo patrón Dapper + <see cref="DbSession"/> que el resto de repositorios de
    /// Connection360.Infrastructure.Persistence.Repositories (ver por ejemplo
    /// ApplicationDataSheetEntregadosRepository); la selección de columnas la resuelve
    /// <see cref="LogStatusTrackingViewColumns"/>.
    /// </summary>
    public class LogStatusTrackingViewRepository : ILogStatusTrackingViewRepository
    {
        private const String ViewName = "connection360read.vw_log_status_tracking";

        private readonly DbSession _session;

        public LogStatusTrackingViewRepository(DbSession session)
        {
            _session = session;
        }

        public async Task<List<LogStatusTrackingViewResultDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);

            String query = $"SELECT {LogStatusTrackingViewColumns.AllColumnsSql} FROM {ViewName};";

            var command = new CommandDefinition(
                query,
                null,
                transaction: _session.Transaction,
                cancellationToken: cancellationToken);

            var result = await _session.Connection.QueryAsync<LogStatusTrackingViewResultDto>(command);
            return result.ToList();
        }

        public async Task<List<LogStatusTrackingViewResultDto>> GetAllAsync(LogStatusTrackingViewFieldsSelectionDto fieldsSelection, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(fieldsSelection);
            await _session.EnsureConnectionOpenAsync(cancellationToken);

            String columns = LogStatusTrackingViewColumns.BuildSelectColumns(fieldsSelection.Fields);
            String query = $"SELECT {columns} FROM {ViewName};";

            var command = new CommandDefinition(
                query,
                null,
                transaction: _session.Transaction,
                cancellationToken: cancellationToken);

            var result = await _session.Connection.QueryAsync<LogStatusTrackingViewResultDto>(command);
            return result.ToList();
        }

        public async Task<List<LogStatusTrackingViewResultDto>> GetByDocumentoTransporteHblAsync(String documentoTransporteHbl, CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);

            String query = $"SELECT {LogStatusTrackingViewColumns.AllColumnsSql} FROM {ViewName} WHERE documento_transporte_hbl = @DocumentoTransporteHbl;";

            var command = new CommandDefinition(
                query,
                new { DocumentoTransporteHbl = documentoTransporteHbl },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken);

            var result = await _session.Connection.QueryAsync<LogStatusTrackingViewResultDto>(command);
            return result.ToList();
        }

        public async Task<List<LogStatusTrackingViewResultDto>> GetByDocumentoTransporteHblAsync(String documentoTransporteHbl, LogStatusTrackingViewFieldsSelectionDto fieldsSelection, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(fieldsSelection);
            await _session.EnsureConnectionOpenAsync(cancellationToken);

            String columns = LogStatusTrackingViewColumns.BuildSelectColumns(fieldsSelection.Fields);
            String query = $"SELECT {columns} FROM {ViewName} WHERE documento_transporte_hbl = @DocumentoTransporteHbl;";

            var command = new CommandDefinition(
                query,
                new { DocumentoTransporteHbl = documentoTransporteHbl },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken);

            var result = await _session.Connection.QueryAsync<LogStatusTrackingViewResultDto>(command);
            return result.ToList();
        }
    }
}
