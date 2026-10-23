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

        /// <summary>Identificador (en Auth0) del nuevo rol del usuario; si se omite, el rol no cambia.</summary>
        public String? RoleId { get; set; }

        /// <summary>Nueva empresa (<c>user_metadata.company</c>).</summary>
        public String? Company { get; set; }

        /// <summary>Nuevo documento de identificación (<c>user_metadata.document</c>).</summary>
        public String? Document { get; set; }

        /// <summary>Nuevo nombre completo (<c>user_metadata.fullName</c>).</summary>
        public String? FullName { get; set; }

        /// <summary>Nuevo teléfono (<c>user_metadata.phone.number</c>).</summary>
        public String? Phone { get; set; }
    }
}
