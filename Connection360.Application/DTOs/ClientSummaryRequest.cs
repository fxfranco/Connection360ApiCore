namespace Connection360.Application.DTOs
{
    /// <summary>
    /// Parámetros de negocio para consultar el resumen (totales/filtros) de envíos de un cliente,
    /// usados por <c>HomeController</c> y <c>ReportsController</c>. El controlador lo construye a
    /// partir de la query string y del rol resuelto del usuario autenticado; no se recibe
    /// directamente del cliente HTTP como cuerpo de la solicitud.
    /// </summary>
    public class ClientSummaryRequest
    {
        /// <summary>Identificador del cliente sobre el que se consulta (dueño de la sesión autenticada).</summary>
        public String IdClient { get; set; } = default!;

        /// <summary>Rol de aplicación del usuario autenticado (ver <see cref="Connection360.Domain.Enums.UserRoleApplication"/>).</summary>
        public String RoleName { get; set; } = default!;

        /// <summary>Valor de texto libre usado para filtrar los resultados (por ejemplo, un número de documento).</summary>
        public String FilterValue { get; set; } = default!;

        /// <summary>
        /// Identificador de un cliente específico a consultar cuando un colaborador/analista necesita
        /// ver los datos de un cliente puntual en vez de todos los que tiene asociados.
        /// </summary>
        public String IdQueryClient { get; set; } = default!;

        /// <summary>
        /// <c>true</c> cuando debe incluirse la información de todos los clientes asociados al
        /// usuario autenticado (no se especificó <see cref="IdQueryClient"/>); <c>false</c> cuando la
        /// consulta debe limitarse a un único cliente.
        /// </summary>
        public Boolean AllClient {  get; set; } = default!;
    }
}
