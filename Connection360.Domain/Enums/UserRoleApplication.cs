namespace Connection360.Domain.Enums
{
    /// <summary>
    /// Roles de aplicación que puede tener un usuario autenticado en Connection360. Se usa tanto
    /// para autorizar endpoints (<c>[Authorize(Roles = "...")]</c>) como para resolver, dentro de
    /// cada controlador, cuál es el rol activo del usuario a partir de sus claims.
    /// </summary>
    public enum UserRoleApplication
    {
        /// <summary>No asignado: No puede acceder ya que no tiene ningún rol asignado.</summary>
        UNASSIGNED,

        /// <summary>Administrador: acceso total a la configuración y a los datos de todos los clientes.</summary>
        ADMIN,

        /// <summary>Cliente final: solo ve la información de su propia cuenta.</summary>
        CLIENT,

        /// <summary>Analista de operaciones.</summary>
        ANALISTAOPE,

        /// <summary>Analista de servicio al cliente (SAC).</summary>
        ANALISTASAC
    }
}
