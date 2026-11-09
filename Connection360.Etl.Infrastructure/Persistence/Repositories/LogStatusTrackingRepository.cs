using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Domain.Ports.Persistence;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Connection360.Etl.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Adaptador del paso "Load" del proceso ETL de logs: inserta filas en PostgreSQL, tabla
    /// connection360write.log_status_tracking (ver Documents/scriptlogsSQL.sql). Mismo patrón
    /// Dapper + <see cref="DbSession"/> que <see cref="ApplicationDataSheetRepository"/>, pero con
    /// INSERT simple (sin ON CONFLICT): la tabla no define ninguna columna UNIQUE aparte de su "id"
    /// autogenerado, así que cada fila se trata como un evento de auditoría independiente.
    /// </summary>
    public class LogStatusTrackingRepository : ILogStatusTrackingRepository
    {
        private readonly DbSession _session;

        public LogStatusTrackingRepository(DbSession session)
        {
            _session = session;
        }

        public async Task<Int32> InsertBatchAsync(IEnumerable<LogStatusTracking> rows, CancellationToken cancellationToken = default)
        {
            var rowsList = rows?.ToList() ?? new List<LogStatusTracking>();
            if (rowsList.Count == 0)
                return 0;

            await _session.EnsureConnectionOpenAsync(cancellationToken);

            const string query = @"
                INSERT INTO connection360write.log_status_tracking
                    (id_operacion, documento_transporte_hbl, fecha_cambio, usuario_cambio, mensaje, estado_anterior, nuevo_estado)
                VALUES
                    (@IdOperacion, @DocumentoTransporteHbl, @FechaCambio, @UsuarioCambio, @Mensaje, @EstadoAnterior, @NuevoEstado);";

            var command = new CommandDefinition(
                query,
                rowsList,
                transaction: _session.Transaction,
                cancellationToken: cancellationToken);

            // Dapper ejecuta la sentencia una vez por cada elemento de rowsList (batch) y devuelve
            // la suma de filas afectadas.
            return await _session.Connection.ExecuteAsync(command);
        }
    }
}
