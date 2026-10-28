namespace Connection360.Application.DTOs.Persistence
{
    /// <summary>
    /// Configuración de canales de notificación de un cliente, ya persistida.
    /// </summary>
    /// <param name="IdNotificationChannels">Identificador del registro de canales de notificación.</param>
    /// <param name="IdCustomer">Identificador del cliente dueño de la configuración.</param>
    /// <param name="Application">Notificaciones dentro de la aplicación (in-app) habilitadas.</param>
    /// <param name="Email">Notificaciones por correo electrónico habilitadas.</param>
    /// <param name="TextMessages">Notificaciones por mensaje de texto (SMS) habilitadas.</param>
    public record CustomerNotificationChannelsResponseDto(Int64 IdNotificationChannels, Int64 IdCustomer, Boolean Application, Boolean Email, Boolean TextMessages);
}
