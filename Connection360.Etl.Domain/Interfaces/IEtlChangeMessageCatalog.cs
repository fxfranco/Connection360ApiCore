using System;

namespace Connection360.Etl.Domain.Interfaces
{
    /// <summary>
    /// Puerto de dominio: catálogo de títulos y mensajes de notificación para los cambios detectados
    /// por <see cref="IApplicationDataSheetChangeDetector"/>. Aislado en su propia interfaz (en vez de
    /// texto embebido directamente en <c>EtlChangeNotifier</c>) para que el contenido de negocio se
    /// pueda ampliar/editar en un único lugar sin tocar la orquestación de la notificación.
    /// </summary>
    public interface IEtlChangeMessageCatalog
    {
        /// <summary>
        /// Título y mensaje para notificar un cambio de estado (ExternalDataFields.State), según el
        /// nuevo estado. El mismo mensaje se usa tanto en connection360write.log_status_tracking.mensaje
        /// como en la propiedad "Message" del payload de connection360write.outbox_messages.
        /// </summary>
        (String Title, String Message) GetStateChangeMessage(String nuevoEstado);

        /// <summary>
        /// Título y mensaje para notificar un cambio de comentario y/o fecha de comentario
        /// (ExternalDataFields.Comment / CommentDate). Solo se usa en connection360write.outbox_messages
        /// (event_type "Comment"): estos cambios no se registran en log_status_tracking.
        /// </summary>
        (String Title, String Message) GetCommentChangeMessage();
    }
}
