using Auth0.ManagementApi;
using Connection360.Domain.Dtos;

namespace Connection360.Infrastructure.Adapters.Output
{
    /// <summary>
    /// Convierte un usuario de la Management API de Auth0 en <see cref="Auth0UserBasicDto"/> (id, documento,
    /// nombre completo, teléfono, correo y rol). Separado de <see cref="Auth0UserService"/> para poder probar la extracción
    /// sin llamar a la red. Documento, nombre completo y teléfono salen de <c>user_metadata</c> (ver
    /// <see cref="Auth0UserMetadataReader"/>): <c>document</c>, <c>fullName</c> y <c>phone.number</c>; el correo es el
    /// atributo <c>email</c> del usuario. El rol NO viene en la respuesta de <c>GET /users</c>: se consulta aparte por usuario
    /// y se completa con <see cref="ApplyRoleName"/> / <see cref="ApplyRoleNames"/>.
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
                Email = user.Email ?? String.Empty,
                RoleName = String.Empty
            };
        }

        /// <summary>
        /// Asigna el nombre del rol al usuario básico. Un nombre nulo o en blanco (usuario sin rol, o sin permiso
        /// para consultarlo) deja el rol vacío; el nombre se recorta.
        /// </summary>
        public static void ApplyRoleName(Auth0UserBasicDto user, String? roleName)
        {
            ArgumentNullException.ThrowIfNull(user);
            user.RoleName = String.IsNullOrWhiteSpace(roleName) ? String.Empty : roleName.Trim();
        }

        /// <summary>
        /// Completa el rol de cada usuario con el diccionario de nombres de rol por id de usuario (resultado de las
        /// consultas de rol). Los usuarios que no aparezcan en el diccionario conservan su rol actual.
        /// </summary>
        public static void ApplyRoleNames(IEnumerable<Auth0UserBasicDto> users, IReadOnlyDictionary<String, String?> roleNamesByUserId)
        {
            ArgumentNullException.ThrowIfNull(users);
            ArgumentNullException.ThrowIfNull(roleNamesByUserId);

            foreach (Auth0UserBasicDto user in users)
            {
                if (roleNamesByUserId.TryGetValue(user.Id, out String? roleName))
                {
                    ApplyRoleName(user, roleName);
                }
            }
        }
    }
}
