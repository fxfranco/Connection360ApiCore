namespace Connection360.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Arma el texto de los SELECT de las vistas de lectura (connection360read.vw_*) en un único lugar.
    ///
    /// Qué va parametrizado y qué no: los VALORES (nit, documento de transporte, etc.) siempre viajan como parámetros de
    /// Dapper (@NitCliente, ...) y nunca se concatenan. Lo único que forma parte del texto SQL son identificadores que
    /// PostgreSQL no permite parametrizar: el nombre de la vista (constante de cada repositorio), la lista de columnas
    /// (sale de una lista blanca por enum, ver <c>ApplicationDataSheetViewColumns</c> y <c>LogStatusTrackingViewColumns</c>)
    /// y el predicado de filtro (texto fijo del código con marcadores @Parametro). Como defensa adicional, este
    /// constructor rechaza cualquier fragmento con caracteres que no sean los de un identificador, una lista de columnas
    /// o un predicado simple, de modo que un texto externo que llegara por error nunca llegaría a la base de datos.
    /// </summary>
    public static class SelectQueryBuilder
    {
        /// <summary>
        /// <c>SELECT {columns} FROM {viewName}[ WHERE {whereClause}];</c>
        /// </summary>
        /// <param name="columns">Lista de columnas ya resuelta desde la lista blanca (admite comas, espacios y el cast ::).</param>
        /// <param name="viewName">Vista con esquema opcional, por ejemplo connection360read.vw_log_status_tracking.</param>
        /// <param name="whereClause">Predicado fijo con parámetros @Nombre (opcional), p. ej. "nit_cliente = @NitCliente".</param>
        public static String Build(String columns, String viewName, String? whereClause = null)
        {
            Validate(columns, nameof(columns), allowed: ", :");
            Validate(viewName, nameof(viewName), allowed: ".");

            if (String.IsNullOrWhiteSpace(whereClause))
            {
                return String.Concat("SELECT ", columns, " FROM ", viewName, ";");
            }

            Validate(whereClause, nameof(whereClause), allowed: " =@.");
            return String.Concat("SELECT ", columns, " FROM ", viewName, " WHERE ", whereClause, ";");
        }

        private static void Validate(String? value, String parameterName, String allowed)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("El fragmento SQL no puede estar vacío.", parameterName);
            }

            foreach (Char c in value)
            {
                if (!Char.IsAsciiLetterOrDigit(c) && c != '_' && !allowed.Contains(c))
                {
                    throw new ArgumentException(
                        $"El fragmento SQL contiene un carácter no permitido ('{c}'); solo se aceptan identificadores y marcadores de parámetro.",
                        parameterName);
                }
            }
        }
    }
}
