using System.ComponentModel.DataAnnotations;

namespace Connection360.Domain.Dtos
{
    public class Auth0UserDto
    {
        public String UserId { get; set; } = String.Empty;
        public String? Email { get; set; } = String.Empty;
        public String? UserName { get; set; } = String.Empty;

        //[Required(ErrorMessage = "El teléfono es obligatorio.")]
        [RegularExpression(@"^\+[0-9]{1,15}$", ErrorMessage = "El formato debe ser E.164 (Ejemplo: +573113232323).")]
        public String? PhoneNumber { get; set; } = String.Empty;
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public Boolean? IsBlocked { get; set; }
        public String? Nickname { get; set; }
    };
}
