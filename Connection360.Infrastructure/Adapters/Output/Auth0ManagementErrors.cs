using Auth0.ManagementApi;

namespace Connection360.Infrastructure.Adapters.Output
{
    /// <summary>
    /// Clasificación de los errores de la Management API de Auth0. El SDK 10 lanza excepciones derivadas de
    /// <see cref="ManagementApiException"/> (por ejemplo <see cref="ForbiddenError"/> para un 403), no
    /// <c>ErrorApiException</c>, que solo existe en el SDK de autenticación.
    /// </summary>
    public static class Auth0ManagementErrors
    {
        /// <summary>Permisos (scopes) que necesita el cliente M2M para consultar los roles de un usuario.</summary>
        public const String RoleLookupScopes = "read:users y read:roles (o read:role_members)";

        /// <summary><c>true</c> si Auth0 respondió 403, p. ej. "Insufficient scope, expected one of: ...".</summary>
        public static Boolean IsForbidden(Exception exception)
        {
            return exception is ManagementApiException { StatusCode: 403 };
        }
    }
}
