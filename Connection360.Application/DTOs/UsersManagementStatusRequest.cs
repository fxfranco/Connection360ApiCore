namespace Connection360.Application.DTOs
{
    /// <summary>
    /// Cuerpo de la solicitud para activar o inactivar un usuario administrado (Auth0).
    /// </summary>
    public class UsersManagementStatusRequest
    {
        /// <summary>
        /// <c>true</c> para activar al usuario (desbloquearlo en Auth0); <c>false</c> para
        /// inactivarlo (bloquearlo: deja de poder iniciar sesión).
        /// </summary>
        public Boolean IsActive { get; set; }
    }
}
