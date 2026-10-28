namespace Connection360.Domain.Dtos
{
    /// <summary>
    /// Rol definido en Auth0 (Management API v2, <c>GET /api/v2/roles</c>). Lo expone
    /// <c>SettingsController</c> para que la interfaz pueda mostrar los roles disponibles y enviar
    /// el <see cref="Id"/> elegido al actualizar un usuario.
    /// </summary>
    public class Auth0RoleDto
    {
        /// <summary>Identificador del rol en Auth0 (por ejemplo, <c>rol_AbC123</c>).</summary>
        public String Id { get; set; } = String.Empty;

        /// <summary>Nombre del rol (por ejemplo, <c>ADMIN</c>, <c>CLIENT</c>).</summary>
        public String Name { get; set; } = String.Empty;

        /// <summary>Descripción del rol.</summary>
        public String? Description { get; set; }
    }
}
