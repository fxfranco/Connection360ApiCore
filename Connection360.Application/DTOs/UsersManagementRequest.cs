namespace Connection360.Application.DTOs
{
    /// <summary>
    /// Parámetros de negocio para listar los usuarios administrables (Auth0) de forma paginada.
    /// </summary>
    public class UsersManagementRequest
    {
        /// <summary>Identificador del usuario, cuando la consulta se limita a uno en particular.</summary>
        public String UserId { get; set; } = default!;

        /// <summary>Rol de aplicación del usuario autenticado que realiza la consulta.</summary>
        public String RoleName { get; set; } = default!;

        /// <summary>Número de página solicitada (base 0).</summary>
        public Int32 Page { get; set; } = default!;

        /// <summary>Cantidad máxima de usuarios por página.</summary>
        public Int32 Size { get; set; } = default!;
    }
}
