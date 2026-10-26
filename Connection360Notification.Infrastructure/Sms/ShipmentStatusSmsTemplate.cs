using Connection360Notification.Domain.Enums;

namespace Connection360Notification.Infrastructure.Sms
{
    /// <summary>
    /// Arma el texto del SMS de un envío según el tipo de notificación (archivo "TextosSMS.txt"). El mensaje tiene tres
    /// líneas: el nombre de la marca, el cuerpo y el lema.
    /// <list type="bullet">
    /// <item><description><see cref="NotificationType.ChangeState"/>: "Tu envío internacional con número de guía {guía} ha actualizado su estado a {nuevo estado}. Consulta los detalles y seguimiento en {URL}".</description></item>
    /// <item><description><see cref="NotificationType.OtherInfo"/>: "... ha actualizado su información. Consulta los detalles y seguimiento en {URL}".</description></item>
    /// <item><description><see cref="NotificationType.Comment"/>: "... se le ha registrado un comentario. Consulta los detalles y seguimiento en {URL}".</description></item>
    /// </list>
    /// Cualquier otro tipo (incluido uno futuro) usa el texto de cambio de estado, igual que la plantilla del correo.
    /// </summary>
    public static class ShipmentStatusSmsTemplate
    {
        /// <summary>Primera línea del SMS.</summary>
        public const String Brand = "Conexión360";

        /// <summary>Última línea del SMS.</summary>
        public const String Slogan = "“Visibilidad total. Decisiones inteligentes”";

        /// <summary>Texto cuando falta el número de guía o el nuevo estado.</summary>
        public const String EmptyValue = "-";

        public static String Render(NotificationType eventType, String? documentNumber, String? newStatus, String? conexion360Url)
        {
            String guide = Clean(documentNumber);
            String link = String.IsNullOrWhiteSpace(conexion360Url) ? Brand : Clean(conexion360Url);

            String body = eventType switch
            {
                NotificationType.OtherInfo =>
                    $"Tu envío internacional con número de guía {guide} ha actualizado su información. Consulta los detalles y seguimiento en {link}",
                NotificationType.Comment =>
                    $"Tu envío internacional con número de guía {guide} se le ha registrado un comentario. Consulta los detalles y seguimiento en {link}",
                _ =>
                    $"Tu envío internacional con número de guía {guide} ha actualizado su estado a {Clean(newStatus)}. Consulta los detalles y seguimiento en {link}"
            };

            return $"{Brand}\n{body}\n{Slogan}";
        }

        /// <summary>Quita saltos de línea y espacios sobrantes (el texto del SMS no debe partirse por un valor externo); vacío => "-".</summary>
        private static String Clean(String? value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                return EmptyValue;
            }

            return String.Join(" ", value.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }
    }
}
