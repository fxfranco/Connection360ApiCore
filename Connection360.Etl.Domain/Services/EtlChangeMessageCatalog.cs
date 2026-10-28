using Connection360.Etl.Domain.Interfaces;
using System;

namespace Connection360.Etl.Domain.Services
{
    /// <summary>
    /// Implementación de <see cref="Interfaces.IEtlChangeMessageCatalog"/>.
    /// <para>
    /// <b>IMPORTANTE - contenido pendiente de confirmar con negocio:</b> por ahora solo se define un
    /// mensaje GENÉRICO (rama <c>_</c> del switch) que aplica a cualquier estado, ya que el catálogo
    /// real de títulos/mensajes por cada valor posible de ESTADO no estaba disponible al implementar
    /// esta funcionalidad. Para personalizar el texto de un estado puntual, basta con agregar un caso
    /// explícito arriba de la rama genérica (ej. <c>"ENTREGADO" =&gt; (Title: "...", Message: "...")</c>),
    /// sin tocar el resto del proceso ETL: el flujo completo de detección/inserción ya está armado para
    /// tomar el texto de aquí sin cambios adicionales.
    /// </para>
    /// </summary>
    public class EtlChangeMessageCatalog : IEtlChangeMessageCatalog
    {
        public (String Title, String Message) GetStateChangeMessage(String nuevoEstado)
        {
            var estado = (nuevoEstado ?? String.Empty).Trim();

            return estado.ToUpperInvariant() switch
            {
                // Pendiente: agregar aquí los casos específicos por estado que defina negocio, por ejemplo:
                // "ENTREGADO" => (Title: "Tu envío fue entregado", Message: "Tu envío llegó a destino."),
                _ => (
                    Title: $"Actualización de estado: {estado}",
                    Message: $"El estado de tu envío cambió a '{estado}'."
                ),
            };
        }

        public (String Title, String Message) GetCommentChangeMessage(String comentario)
        {
            var texto = (comentario ?? String.Empty).Trim();

            // El título es fijo; solo el mensaje incluye el comentario.
            return (
                Title: "Nuevo comentario en tu envío",
                Message: $"Se registró nuevo comentario '{texto}' en el seguimiento de tu envío"
            );
        }

        public (String Title, String Message) GetOtherInfoChangeMessage()
        {
            return (
                Title: "Actualización información de tu envío",
                Message: "Se registró un cambio en la información general de tu envío"
            );
        }
    }
}
