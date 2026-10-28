namespace Connection360.Application.DTOs
{
    /// <summary>
    /// Parámetros de negocio para consultar/filtrar "mis envíos" y su historial, usados por
    /// <c>MyShipmentsController</c>. El controlador lo construye a partir de la query string y del
    /// rol resuelto del usuario autenticado.
    /// </summary>
    public class MyShipmentsRequest
    {
        /// <summary>Identificador del cliente sobre el que se consulta.</summary>
        public String IdClient { get; set; } = default!;

        /// <summary>Rol de aplicación del usuario autenticado (ver <see cref="Connection360.Domain.Enums.UserRoleApplication"/>).</summary>
        public String RoleName { get; set; } = default!;

        /// <summary>Número de documento del envío a consultar en detalle.</summary>
        public String DocumentNumber { get; set; } = default!;

        /// <summary>Número de página solicitada (base 0).</summary>
        public Int64 Page { get; set; } = default!;

        /// <summary>Cantidad máxima de elementos por página.</summary>
        public Int64 Size { get; set; } = default!;

        /// <summary>Filtros adicionales opcionales aplicados a la consulta.</summary>
        public MyShipmentsFiltersRequest? Filters { get; set; }

        /// <summary>Identificador de un cliente específico a consultar (ver <see cref="ClientSummaryRequest.IdQueryClient"/>).</summary>
        public String IdQueryClient { get; set; } = default!;

        /// <summary>
        /// <c>true</c> cuando debe incluirse la información de todos los clientes asociados al
        /// usuario autenticado; <c>false</c> cuando la consulta debe limitarse a un único cliente.
        /// </summary>
        public Boolean AllClient { get; set; } = default!;
    }
}
