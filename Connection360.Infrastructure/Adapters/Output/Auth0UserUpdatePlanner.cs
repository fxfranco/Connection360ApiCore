namespace Connection360.Infrastructure.Adapters.Output
{
    /// <summary>
    /// Reglas para decidir qué atributos de un usuario de Auth0 hay que enviar al actualizarlo.
    /// Se separa de <see cref="Auth0UserService"/> porque no depende de la red y así se puede probar.
    /// </summary>
    public static class Auth0UserUpdatePlanner
    {
        /// <summary>
        /// Indica si el valor solicitado debe enviarse a Auth0: ocurre cuando se informó (no es nulo, vacío
        /// ni espacios) y es distinto del valor actual. Un valor igual al actual no se reenvía, porque Auth0
        /// puede rechazar la actualización de atributos que su conexión no permite modificar aunque no cambien.
        /// </summary>
        /// <param name="requested">Valor recibido del cliente.</param>
        /// <param name="current">Valor que hoy tiene el usuario en Auth0.</param>
        /// <param name="ignoreCase">Si la comparación ignora mayúsculas/minúsculas (p. ej. correos).</param>
        public static Boolean HasChanged(String? requested, String? current, Boolean ignoreCase)
        {
            if (String.IsNullOrWhiteSpace(requested))
            {
                return false;
            }

            StringComparison comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return !String.Equals(requested.Trim(), (current ?? String.Empty).Trim(), comparison);
        }
    }
}
