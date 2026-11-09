namespace Connection360.Application.DTOs.Persistence
{
    /// <summary>
    /// Datos para crear la configuración de canales de notificación de un cliente.
    /// </summary>
    /// <param name="IdCustomer">Identificador del cliente dueño de la configuración.</param>
    /// <param name="Application">Habilita las notificaciones dentro de la aplicación (in-app).</param>
    /// <param name="Email">Habilita las notificaciones por correo electrónico.</param>
    /// <param name="TextMessages">Habilita las notificaciones por mensaje de texto (SMS).</param>
    public record CreateCustomerNotificationChannelsDto(Int64 IdCustomer, Boolean Application, Boolean Email, Boolean TextMessages);
}
