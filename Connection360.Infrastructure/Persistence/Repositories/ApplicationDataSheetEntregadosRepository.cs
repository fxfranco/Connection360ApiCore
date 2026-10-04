using Connection360.Domain.Dtos;
using Connection360.Domain.Ports.Persistence;
using Dapper;

namespace Connection360.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Acceso de solo lectura a connection360read.vw_application_data_sheet_entregados (ver Documents/vw_application_data_sheet_entregados.sql). Sigue el mismo
    /// patrón Dapper + <see cref="DbSession"/> que el resto de repositorios de
    /// Connection360.Infrastructure.Persistence.Repositories (ver por ejemplo CustomerRepository);
    /// la selección de columnas la resuelve <see cref="ApplicationDataSheetViewColumns"/>, compartida
    /// con <see cref="ApplicationDataSheetNoEntregadosRepository"/>.
    /// </summary>
    public class ApplicationDataSheetEntregadosRepository : IApplicationDataSheetEntregadosRepository
    {
        private const String ViewName = "connection360read.vw_application_data_sheet_entregados";

        private readonly DbSession _session;

        public ApplicationDataSheetEntregadosRepository(DbSession session)
        {
            _session = session;
        }

        public async Task<List<ApplicationDataSheetViewResultDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);

            String query = $"SELECT {ApplicationDataSheetViewColumns.AllColumnsSql} FROM {ViewName};";

            var command = new CommandDefinition(
                query,
                null,
                transaction: _session.Transaction,
                cancellationToken: cancellationToken);

            var result = await _session.Connection.QueryAsync<ApplicationDataSheetViewResultDto>(command);
            return result.ToList();
        }

        public async Task<List<ApplicationDataSheetViewResultDto>> GetAllAsync(ApplicationDataSheetViewFieldsSelectionDto fieldsSelection, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(fieldsSelection);
            await _session.EnsureConnectionOpenAsync(cancellationToken);

            String columns = ApplicationDataSheetViewColumns.BuildSelectColumns(fieldsSelection.Fields);
            String query = $"SELECT {columns} FROM {ViewName};";

            var command = new CommandDefinition(
                query,
                null,
                transaction: _session.Transaction,
                cancellationToken: cancellationToken);

            var result = await _session.Connection.QueryAsync<ApplicationDataSheetViewResultDto>(command);
            return result.ToList();
        }

        public async Task<List<ApplicationDataSheetViewResultDto>> GetByNitClienteAsync(String nitCliente, CancellationToken cancellationToken = default)
        {
            await _session.EnsureConnectionOpenAsync(cancellationToken);

            String query = $"SELECT {ApplicationDataSheetViewColumns.AllColumnsSql} FROM {ViewName} WHERE nit_cliente = @NitCliente;";

            var command = new CommandDefinition(
                query,
                new { NitCliente = nitCliente },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken);

            var result = await _session.Connection.QueryAsync<ApplicationDataSheetViewResultDto>(command);
            return result.ToList();
        }

        public async Task<List<ApplicationDataSheetViewResultDto>> GetByNitClienteAsync(String nitCliente, ApplicationDataSheetViewFieldsSelectionDto fieldsSelection, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(fieldsSelection);
            await _session.EnsureConnectionOpenAsync(cancellationToken);

            String columns = ApplicationDataSheetViewColumns.BuildSelectColumns(fieldsSelection.Fields);
            String query = $"SELECT {columns} FROM {ViewName} WHERE nit_cliente = @NitCliente;";

            var command = new CommandDefinition(
                query,
                new { NitCliente = nitCliente },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken);

            var result = await _session.Connection.QueryAsync<ApplicationDataSheetViewResultDto>(command);
            return result.ToList();
        }
    }
}
