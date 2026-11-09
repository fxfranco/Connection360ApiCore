using Connection360.Domain.Enums;

namespace Connection360.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Mapeo de <see cref="LogStatusTrackingViewField"/> al fragmento SQL de cada columna de
    /// connection360read.vw_log_status_tracking (ver Documents/vw_log_status_tracking.sql). No hacen
    /// falta alias "AS" porque Connection360.Api/Program.cs habilita
    /// Dapper.DefaultTypeMap.MatchNamesWithUnderscores: el mapeo a cada propiedad PascalCase de
    /// LogStatusTrackingViewResultDto es automático. A diferencia de application_data_sheet, aquí no
    /// hay cast ::timestamp: fecha_cambio es TIMESTAMP (no DATE), que Npgsql ya devuelve como
    /// System.DateTime.
    /// </summary>
    internal static class LogStatusTrackingViewColumns
    {
        private static readonly (LogStatusTrackingViewField Field, String Sql)[] _columns =
        {
            (LogStatusTrackingViewField.Id, "id"),
            (LogStatusTrackingViewField.IdOperacion, "id_operacion"),
            (LogStatusTrackingViewField.DocumentoTransporteHbl, "documento_transporte_hbl"),
            (LogStatusTrackingViewField.FechaCambio, "fecha_cambio"),
            (LogStatusTrackingViewField.UsuarioCambio, "usuario_cambio"),
            (LogStatusTrackingViewField.Mensaje, "mensaje"),
            (LogStatusTrackingViewField.EstadoAnterior, "estado_anterior"),
            (LogStatusTrackingViewField.NuevoEstado, "nuevo_estado"),
        };

        /// <summary>Todas las columnas de la vista, en el orden de <see cref="LogStatusTrackingViewField"/>.</summary>
        public static readonly String AllColumnsSql = String.Join(", ", _columns.Select(c => c.Sql));

        /// <summary>
        /// Arma la lista de columnas del SELECT para los campos pedidos, siempre en el orden fijo de
        /// la vista (y sin duplicados), a partir de una lista blanca de columnas: nunca se concatena
        /// texto recibido de afuera dentro del SQL.
        /// </summary>
        public static String BuildSelectColumns(IEnumerable<LogStatusTrackingViewField> fields)
        {
            HashSet<LogStatusTrackingViewField> requested = fields is null
                ? new HashSet<LogStatusTrackingViewField>()
                : new HashSet<LogStatusTrackingViewField>(fields);

            if (requested.Count == 0)
            {
                throw new ArgumentException(
                    "LogStatusTrackingViewFieldsSelectionDto.Fields debe traer al menos un campo.",
                    nameof(fields));
            }

            List<String> selected = _columns.Where(c => requested.Contains(c.Field)).Select(c => c.Sql).ToList();
            return String.Join(", ", selected);
        }
    }
}
