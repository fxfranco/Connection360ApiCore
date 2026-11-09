using Connection360Notification.Domain;

namespace Connection360Notification.Application.Services
{
    /// <summary>
    /// Datos del destinatario (<see cref="BasicUserData"/>: Document, FullName, Phone y Email) de la notificación que se
    /// está procesando. Su ciclo de vida es "por ámbito" (scoped): el consumidor de Kafka crea un ámbito por mensaje, así
    /// que cada notificación ve únicamente su propio destinatario.
    ///
    /// ProcessIncomingNotificationUseCase lo llena (consultando la colección <c>basicUserData</c> con el
    /// <c>ClientId</c>) justo antes de despachar, y los canales (p. ej. el de correo) lo leen para personalizar el envío
    /// sin tener que consultar la base de datos ni cambiar el contrato de INotificationChannel.
    /// </summary>
    public interface INotificationRecipientContext
    {
        /// <summary>
        /// Datos del destinatario, o <c>null</c> si todavía no se resolvieron o si no existe un usuario cuyo documento
        /// sea igual al ClientId (los canales deben seguir funcionando con sus valores por defecto).
        /// </summary>
        BasicUserData? Recipient { get; }

        /// <summary>Guarda (o limpia, con <c>null</c>) el destinatario de la notificación en curso.</summary>
        void Set(BasicUserData? recipient);
    }
}
