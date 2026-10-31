namespace Connection360.Domain.Dtos
{
    /// <summary>
    /// Datos básicos de un usuario de Auth0 que se sincronizan hacia Connection360Notification.Api
    /// (colección <c>basicUserData</c>). Document, FullName y Phone salen de <c>user_metadata</c>; Id y Email son atributos del usuario
    /// y RoleName es el nombre del rol asignado en Auth0 (se consulta por usuario).
    /// </summary>
    public class Auth0UserBasicDto
    {
        /// <summary>Identificador único del usuario en Auth0 (<c>user_id</c>).</summary>
        public String Id { get; set; } = String.Empty;

        /// <summary>Documento de identificación (NIT/cédula) del usuario (<c>user_metadata.document</c>), el mismo que se usa como <c>idClient</c> en la API.</summary>
        public String Document { get; set; } = String.Empty;

        /// <summary>Nombre completo del usuario (<c>user_metadata.fullName</c>).</summary>
        public String FullName { get; set; } = String.Empty;

        /// <summary>Teléfono del usuario (<c>user_metadata.phone.number</c>).</summary>
        public String Phone { get; set; } = String.Empty;

        /// <summary>Correo electrónico del usuario (atributo <c>email</c> de Auth0).</summary>
        public String Email { get; set; } = String.Empty;

        /// <summary>
        /// Nombre del rol asignado al usuario en Auth0 (por ejemplo <c>ADMIN</c> o <c>CLIENT</c>). Queda vacío si el usuario
        /// no tiene rol o si la aplicación M2M no tiene permiso para consultar roles (<c>read:roles</c>).
        /// </summary>
        public String RoleName { get; set; } = String.Empty;
    }
}
