namespace Connection360Notification.Domain.Enums
{
    /// <summary>
    /// Canales por los que se puede entregar una notificación. Para agregar uno nuevo (p. ej. WhatsApp):
    /// 1) agregar el valor aquí, 2) agregar su indicador en <see cref="NotificationChannelPreferences"/>
    /// (y en <c>IsEnabled</c>/<c>GetEnabledChannels</c>) y 3) crear su implementación de
    /// <c>INotificationChannel</c> en Infrastructure y registrarla en DI. Application no cambia.
    /// </summary>
    public enum NotificationChannelType
    {
        /// <summary>Notificación dentro de la aplicación en tiempo real (SignalR). Es el canal por defecto.</summary>
        Application,

        /// <summary>Correo electrónico.</summary>
        Email,

        /// <summary>Mensaje de texto (SMS).</summary>
        TextMessages
    }
}
