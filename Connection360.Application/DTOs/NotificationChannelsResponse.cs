using System;
using System.Collections.Generic;
using System.Text;

namespace Connection360.Application.DTOs
{
    /// <summary>
    /// Canales de notificación habilitados para un cliente, expuestos dentro de
    /// <see cref="NotificationsSettingsResponse.NotificationChannels"/>.
    /// </summary>
    public class NotificationChannelsResponse
    {
        /// <summary>Identificador del cliente dueño de la configuración.</summary>
        public String ClientId { get; set; }

        /// <summary>Identificador del registro de canales de notificación.</summary>
        public Int64 NotificationChannelId { get; set; }

        /// <summary>Notificaciones dentro de la aplicación (in-app).</summary>
        public Boolean Application {  get; set; }

        /// <summary>Notificaciones por correo electrónico.</summary>
        public Boolean Email {  get; set; }

        /// <summary>Notificaciones por mensaje de texto (SMS).</summary>
        public Boolean TextMessages {  get; set; }
    }
}
