namespace Connection360.Application.DTOs
{
    /// <summary>
    /// Datos editables de un usuario administrado (Auth0). Todas las propiedades salvo
    /// <see cref="UserId"/> son opcionales: solo se actualizan los campos enviados.
    /// </summary>
    public class UsersManagementUpdateRequest
    {
        /// <summary>Identificador del usuario a actualizar.</summary>
        public String UserId { get; set; } = String.Empty;

        /// <summary>Nuevo correo electrónico del usuario.</summary>
        public String? Email { get; set; } = String.Empty;

        /// <summary>Nuevo nombre de usuario.</summary>
        public String? UserName { get; set; } = String.Empty;

        /// <summary>Nuevo número de teléfono del usuario, en formato E.164 (por ejemplo, +573113232323).</summary>
        public String? PhoneNumber { get; set; } = String.Empty;

        /// <summary>Fecha de creación del usuario.</summary>
        public DateTime? CreatedDate { get; set; }

        /// <summary>Fecha de la última actualización del usuario.</summary>
        public DateTime? UpdatedDate { get; set; }

        /// <summary><c>true</c> si el usuario se encuentra bloqueado.</summary>
        public Boolean? IsBlocked { get; set; }
    }
}
