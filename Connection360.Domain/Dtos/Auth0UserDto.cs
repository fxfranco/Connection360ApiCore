using System.ComponentModel.DataAnnotations;

namespace Connection360.Domain.Dtos
{
    /// <summary>
    /// Datos de un usuario administrado en Auth0. Es la forma que expone
    /// <c>SettingsController</c> para listar, consultar y actualizar usuarios
    /// (<c>GetUsersListSettings</c>, <c>GetUsersByIdSettings</c>, <c>UpdateUsersByIdSettings</c>).
    /// </summary>
    public class Auth0UserDto
    {
        /// <summary>Identificador único del usuario en Auth0.</summary>
        public String UserId { get; set; } = String.Empty;

        /// <summary>Correo electrónico del usuario.</summary>
        public String? Email { get; set; } = String.Empty;

        /// <summary>Nombre de usuario.</summary>
        public String? UserName { get; set; } = String.Empty;

        /// <summary>Número de teléfono del usuario, en formato E.164 (por ejemplo, +573113232323).</summary>
        //[Required(ErrorMessage = "El teléfono es obligatorio.")]
        [RegularExpression(@"^\+[0-9]{1,15}$", ErrorMessage = "El formato debe ser E.164 (Ejemplo: +573113232323).")]
        public String? PhoneNumber { get; set; } = String.Empty;

        /// <summary>Fecha de creación del usuario en Auth0.</summary>
        public DateTime? CreatedDate { get; set; }

        /// <summary>Fecha de la última actualización del usuario en Auth0.</summary>
        public DateTime? UpdatedDate { get; set; }

        /// <summary><c>true</c> si el usuario se encuentra bloqueado.</summary>
        public Boolean? IsBlocked { get; set; }

        /// <summary>Apodo (nickname) del usuario en Auth0.</summary>
        public String? Nickname { get; set; }
    };
}
