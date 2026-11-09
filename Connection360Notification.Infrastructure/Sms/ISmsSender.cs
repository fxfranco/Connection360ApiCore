namespace Connection360Notification.Infrastructure.Sms
{
    /// <summary>Mensaje de texto listo para enviar: celular de destino en formato internacional (solo dígitos) y contenido.</summary>
    /// <param name="Recipient">Celular con indicativo de país, sin "+" (p. ej. 573113232323).</param>
    /// <param name="Content">Texto del SMS.</param>
    public sealed record SmsMessage(String Recipient, String Content);

    /// <summary>
    /// Puerto de salida del envío de SMS. <see cref="Adapters.Output.SmsNotificationChannel"/> depende solo de esta
    /// abstracción; hoy la implementa Brevo (<see cref="BrevoSmsSender"/>) y otro proveedor solo requeriría un nuevo adaptador.
    /// </summary>
    public interface ISmsSender
    {
        /// <summary>
        /// Envía el SMS. Devuelve el identificador del mensaje que asigna el proveedor (o <c>null</c> si no lo informó) y
        /// lanza una excepción si el proveedor rechaza o no recibe el envío.
        /// </summary>
        Task<String?> SendAsync(SmsMessage message, CancellationToken cancellationToken);
    }
}
