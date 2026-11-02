namespace Connection360Notification.Domain
{
    /// <summary>
    /// Nombres de los roles de Auth0 (campo <see cref="BasicUserData.RoleName"/>) que el servicio de notificaciones necesita
    /// conocer para decidir a quién entregar una notificación.
    /// </summary>
    public static class BasicUserRoles
    {
        /// <summary>
        /// Rol administrador: recibe las alertas del sistema (<see cref="Enums.NotificationType.Alert"/>), que no pertenecen a
        /// ningún cliente. La comparación con <see cref="BasicUserData.RoleName"/> no distingue mayúsculas de minúsculas.
        /// </summary>
        public const String Admin = "ADMIN";
    }
}
