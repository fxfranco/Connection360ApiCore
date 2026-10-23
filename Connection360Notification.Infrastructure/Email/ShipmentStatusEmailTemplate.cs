using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Connection360Notification.Infrastructure.Email
{
    /// <summary>
    /// Valores que se insertan en la plantilla del correo de cambio de estado de un envío
    /// (<c>Email/Templates/ShipmentStatusEmail.html</c>, tomada del documento "Plantilla notificación correo.docx").
    /// </summary>
    /// <param name="ClientName">Nombre completo del cliente (FullName de basicUserData). En blanco => "cliente".</param>
    /// <param name="DocumentNumber">Número de guía (DocumentNumber de la notificación). En blanco => "-".</param>
    /// <param name="Origin">Ciudad / país de origen.</param>
    /// <param name="Destination">Ciudad / país de destino.</param>
    /// <param name="PreviousStatus">Estado anterior del envío.</param>
    /// <param name="NewStatus">Nuevo estado del envío.</param>
    /// <param name="Comment">Comentario del cambio de estado.</param>
    /// <param name="UpdatedAt">Fecha y hora (MessageDate de la notificación).</param>
    /// <param name="Conexion360Url">URL de Conexion360 (SmtpSettings:Conexion360Url). Solo se vuelve enlace si es http/https.</param>
    public sealed record ShipmentStatusEmailValues(
        String? ClientName,
        String? DocumentNumber,
        String Origin,
        String Destination,
        String PreviousStatus,
        String NewStatus,
        String Comment,
        DateTime UpdatedAt,
        String? Conexion360Url);

    /// <summary>
    /// Arma el cuerpo HTML del correo de cambio de estado a partir de la plantilla incrustada en el ensamblado. El texto
    /// fijo viene de la plantilla; solo los campos marcados con <c>{{Nombre}}</c> cambian. Todo valor variable se codifica
    /// como HTML (nada de lo que llegue en la notificación o en la base se interpreta como marcado) y la sustitución se
    /// hace en una sola pasada, de modo que un valor que contenga <c>{{...}}</c> no se vuelve a expandir.
    /// </summary>
    public static class ShipmentStatusEmailTemplate
    {
        /// <summary>Content-ID con el que el logo se incrusta en el correo (<c>cid:</c>).</summary>
        public const String LogoContentId = "logoConexion360";

        /// <summary>Nombre del recurso incrustado con el logo (archivo <c>logoConexion360.png</c> de la carpeta Documents).</summary>
        public const String LogoResourceName = "logoConexion360.png";

        public const String TemplateResourceName = "ShipmentStatusEmail.html";

        public const String LogoMediaType = "image/png";

        /// <summary>Texto cuando el cliente no tiene nombre registrado (no existe en basicUserData).</summary>
        public const String DefaultClientName = "cliente";

        /// <summary>Texto cuando la notificación no trae número de guía.</summary>
        public const String EmptyValue = "-";

        // Valores provisionales hasta que el evento de negocio traiga estos datos. Reemplazarlos aquí o, cuando existan
        // en el mensaje, pasarlos desde EmailNotificationChannel.
        public const String PendingOrigin = "ORIGENPENDIENTE";
        public const String PendingDestination = "DESTINOPENDIENTE";
        public const String PendingPreviousStatus = "ESTADOANTERIORPENDIENTE";
        public const String PendingNewStatus = "ESTADONUEVOPENDIENTE";
        public const String PendingComment = "COMENTARIOPENDIENTE";

        private static readonly Regex TokenPattern = new(@"\{\{(\w+)\}\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Lazy<String> Template = new(() => ReadResourceText(TemplateResourceName));

        private static readonly Lazy<Byte[]> Logo = new(() => ReadResourceBytes(LogoResourceName));

        /// <summary>Bytes del logo incrustado (PNG).</summary>
        public static Byte[] GetLogo() => Logo.Value;

        /// <summary>Fecha y hora con el formato del correo: <c>dd/MM/yyyy HH:mm:ss</c> y " UTC" si la fecha es UTC.</summary>
        public static String FormatDate(DateTime value)
        {
            String text = value.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            return value.Kind == DateTimeKind.Utc ? text + " UTC" : text;
        }

        public static String Render(ShipmentStatusEmailValues values)
        {
            ArgumentNullException.ThrowIfNull(values);

            var replacements = new Dictionary<String, String>(StringComparer.Ordinal)
            {
                ["ClienteNombre"] = Encode(Or(values.ClientName, DefaultClientName)),
                ["NumeroGuia"] = Encode(Or(values.DocumentNumber, EmptyValue)),
                ["Origen"] = Encode(values.Origin),
                ["Destino"] = Encode(values.Destination),
                ["EstadoAnterior"] = Encode(values.PreviousStatus),
                ["NuevoEstado"] = Encode(values.NewStatus),
                ["Comentario"] = Encode(values.Comment),
                ["FechaHora"] = Encode(FormatDate(values.UpdatedAt)),
                ["UrlConexion360"] = BuildUrlHtml(values.Conexion360Url),
                ["LogoContentId"] = LogoContentId
            };

            return TokenPattern.Replace(Template.Value, match =>
                replacements.TryGetValue(match.Groups[1].Value, out String? replacement) ? replacement : match.Value);
        }

        /// <summary>
        /// Enlace con la URL (como en la plantilla, el texto es la propia URL). Si no es una URL http/https válida no se
        /// genera enlace (evita esquemas como javascript:) y se muestra solo el texto codificado, o nada si está vacía.
        /// </summary>
        private static String BuildUrlHtml(String? url)
        {
            if (String.IsNullOrWhiteSpace(url))
            {
                return String.Empty;
            }

            String trimmed = url.Trim();
            if (Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
            {
                String encoded = Encode(trimmed);
                return $"<a href=\"{encoded}\" style=\"color:#0563C1;text-decoration:underline;\">{encoded}</a>";
            }

            return Encode(trimmed);
        }

        private static String Or(String? value, String fallback) => String.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

        private static String Encode(String? value) => WebUtility.HtmlEncode(value ?? String.Empty);

        private static String ReadResourceText(String name)
        {
            using Stream stream = OpenResource(name);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private static Byte[] ReadResourceBytes(String name)
        {
            using Stream stream = OpenResource(name);
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        }

        private static Stream OpenResource(String name)
        {
            return typeof(ShipmentStatusEmailTemplate).GetTypeInfo().Assembly.GetManifestResourceStream(name)
                ?? throw new InvalidOperationException($"No se encontró el recurso incrustado '{name}' del correo.");
        }
    }
}
