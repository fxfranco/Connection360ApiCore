using System;

namespace Connection360.Etl.Domain.Entities
{
    /// <summary>
    /// Canales de notificación habilitados para un cliente, tal como están en
    /// connection360write.customer_notification_channels (application, email, text_messages).
    /// Es un valor de solo lectura: la ETL únicamente los consulta para incluirlos en el payload
    /// de outbox_messages, nunca los modifica.
    /// </summary>
    public sealed record CustomerNotificationChannelSettings(Boolean Application, Boolean Email, Boolean TextMessages)
    {
        /// <summary>
        /// Valor a usar cuando el cliente no tiene configuración de canales (o ni siquiera existe
        /// en connection360write.customers): todos los canales en false.
        /// </summary>
        public static CustomerNotificationChannelSettings None { get; } = new(false, false, false);
    }
}
