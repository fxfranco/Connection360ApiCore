using Auth0.ManagementApi;
using Connection360.Domain.Dtos;

namespace Connection360.Infrastructure.Adapters.Output
{
    /// <summary>
    /// Convierte un usuario de la Management API de Auth0 en <see cref="Auth0UserBasicDto"/> (id, documento,
    /// nombre completo, teléfono y correo). Separado de <see cref="Auth0UserService"/> para poder probar la extracción
    /// sin llamar a la red. Documento, nombre completo y teléfono salen de <c>user_metadata</c> (ver
    /// <see cref="Auth0UserMetadataReader"/>): <c>document</c>, <c>fullName</c> y <c>phone.number</c>; el correo es el
    /// atributo <c>email</c> del usuario.
    /// </summary>
    public static class Auth0UserBasicMapper
    {
        /// <summary>Campos que se piden a Auth0 (parámetro <c>fields</c>) para no traer el usuario completo.</summary>
        public const String RequestedFields = "user_id,email,user_metadata";

        public static Auth0UserBasicDto ToBasicDto(UserResponseSchema user)
        {
            return new Auth0UserBasicDto
            {
                Id = user.UserId ?? String.Empty,
                Document = Auth0UserMetadataReader.GetString(user.UserMetadata, Auth0UserMetadataReader.DocumentKey) ?? String.Empty,
                FullName = Auth0UserMetadataReader.GetString(user.UserMetadata, Auth0UserMetadataReader.FullNameKey) ?? String.Empty,
                Phone = Auth0UserMetadataReader.GetPhoneNumber(user.UserMetadata) ?? String.Empty,
                Email = user.Email ?? String.Empty
            };
        }
    }
}
