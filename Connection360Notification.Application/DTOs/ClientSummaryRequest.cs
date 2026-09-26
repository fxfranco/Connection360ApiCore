namespace Connection360Notification.Application.DTOs
{
    /// <summary>
    /// Filtros para consultar las notificaciones asociadas a un cliente (o a varios, según el rol).
    /// </summary>
    public class ClientSummaryRequest
    {
        //TODO: Pendiente cambiar nombre para no confundir 
        /// <summary>Identificador del cliente sobre el que se consulta. Puede quedar vacío cuando <see cref="AllClient"/> es <c>true</c> o el rol del usuario permite ver todos los clientes.</summary>
        public String IdClient { get; set; } = default!;

        /// <summary>Rol del usuario autenticado que realiza la consulta; determina si puede ver notificaciones de más de un cliente.</summary>
        public String RoleName { get; set; } = default!;

        /// <summary>Valor de filtro adicional aplicado sobre la búsqueda de notificaciones (por ejemplo, texto libre).</summary>
        public String FilterValue { get; set; } = default!;

        /// <summary>Identificador de cliente adicional usado para acotar la consulta cuando el rol del usuario requiere filtrar por otro cliente distinto de <see cref="IdClient"/>.</summary>
        public String IdQueryClient { get; set; } = default!;

        /// <summary>Indica si la consulta debe incluir notificaciones de todos los clientes en lugar de uno específico.</summary>
        public Boolean AllClient {  get; set; } = default!;
    }
}
