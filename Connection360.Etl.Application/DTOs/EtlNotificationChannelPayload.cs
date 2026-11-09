using System;

namespace Connection360.Etl.Application.DTOs
{
    /// <summary>
    /// Canales de notificación habilitados del cliente, serializados dentro de
    /// <see cref="EtlOutboxPayload.NotificationChannel"/>. Todos en false cuando el cliente no tiene
    /// configuración de canales.
    /// </summary>
    public sealed class EtlNotificationChannelPayload
    {
        public Boolean Application { get; set; }
        public Boolean Email { get; set; }
        public Boolean TextMessages { get; set; }
    }
}
