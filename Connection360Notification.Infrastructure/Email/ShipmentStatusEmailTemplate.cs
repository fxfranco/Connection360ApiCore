using Connection360Notification.Domain.Enums;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Connection360Notification.Infrastructure.Email
{
    /// <summary>
    /// Valores que se insertan en la plantilla del correo de un envío
    /// (<c>Email/Templates/ShipmentStatusEmail.html</c>, tomada del documento "Plantilla notificación correo.docx").
    /// El tipo de notificación decide qué secciones se muestran (ver <see cref="ShipmentStatusEmailTemplate"/>).
    /// </summary>
    /// <param name="ClientName">Nombre completo del cliente (FullName de basicUserData). En blanco => "cliente".</param>
    /// <param name="DocumentNumber">Número de guía (DocumentNumber de la notificación). En blanco => "-".</param>
    /// <param name="EventType">Tipo de notificación (EventType): define el texto de introducción y las secciones visibles.</param>
    /// <param name="Origin">Ciudad / país de origen (Origen). En blanco => "-".</param>
    /// <param name="Destination">Ciudad / país de destino (Destino). En blanco => "-".</param>
    /// <param name="PreviousStatus">Estado anterior del envío (EstadoAnterior). En blanco => "-".</param>
    /// <param name="NewStatus">Estado vigente del envío (NuevoEstado). En blanco => "-".</param>
    /// <param name="Comment">Comentario vigente (Comentario). En blanco => "-".</param>
    /// <param name="CommentDate">Fecha del comentario (FechaComentario). <c>default</c> => "-".</param>
    /// <param name="UpdatedAt">Fecha y hora (MessageDate de la notificación).</param>
    /// <param name="Conexion360Url">URL de Conexion360 (SmtpSettings:Conexion360Url). Solo se vuelve enlace si es http/https.</param>
    public sealed record ShipmentStatusEmailValues(
        String? ClientName,
        String? DocumentNumber,
        NotificationType EventType,
        String? Origin,
        String? Destination,
        String? PreviousStatus,
        String? NewStatus,
        String? Comment,
        DateTime CommentDate,
        DateTime UpdatedAt,
        String? Conexion360Url);

    /// <summary>
    /// Arma el cuerpo HTML del correo a partir de la plantilla incrustada en el ensamblado. El texto
    /// fijo viene de la plantilla; solo los campos marcados con <c>{{Nombre}}</c> cambian. Las secciones opcionales van entre
    /// <c>{{#Nombre}}...{{/Nombre}}</c> y se incluyen o se quitan según el tipo de notificación:
    /// <list type="bullet">
    /// <item><description><see cref="NotificationType.ChangeState"/>: "Estado anterior" y "Nuevo estado"; sin Comentario ni Fecha comentario.</description></item>
    /// <item><description><see cref="NotificationType.Comment"/>: un único "Estado" (el nuevo estado) más Comentario y Fecha comentario.</description></item>
    /// <item><description><see cref="NotificationType.OtherInfo"/>: un único "Estado" (el nuevo estado); sin Comentario ni Fecha comentario.</description></item>
    /// </list>
    /// El texto de introducción también cambia con el tipo. Todo valor variable se codifica
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

        /// <summary>Introducción cuando el correo informa un cambio de estado (completa "Le informamos que ... <b>Conexion360</b>.").</summary>
        public const String IntroChangeState = "el estado de su envío de carga internacional ha sido actualizado en";

        /// <summary>Introducción cuando el correo informa un comentario nuevo.</summary>
        public const String IntroComment = "un comentario ha sido ingresado en";

        /// <summary>Introducción cuando el correo informa un cambio en la información general del envío.</summary>
        public const String IntroOtherInfo = "la información de su envío de carga internacional ha sido actualizada en";

        // Sección opcional: {{#Nombre}}contenido{{/Nombre}}. La referencia inversa (\1) exige que cierre con el mismo nombre.
        private static readonly Regex SectionPattern = new(@"\{\{#(\w+)\}\}(.*?)\{\{/\1\}\}", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

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

        /// <summary>
        /// Fecha del comentario: <see cref="EmptyValue"/> si no hay fecha; solo <c>dd/MM/yyyy</c> si no trae hora (la columna
        /// fecha_comentario es DATE); con hora y " UTC" si es UTC como <see cref="FormatDate"/>.
        /// </summary>
        public static String FormatCommentDate(DateTime value)
        {
            if (value == default)
            {
                return EmptyValue;
            }

            return value.TimeOfDay == TimeSpan.Zero
                ? value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                : FormatDate(value);
        }

        public static String Render(ShipmentStatusEmailValues values)
        {
            ArgumentNullException.ThrowIfNull(values);

            // Cualquier tipo distinto de Comment/OtherInfo (incluido uno futuro) conserva el formato original de cambio de estado.
            Boolean isComment = values.EventType == NotificationType.Comment;
            Boolean isOtherInfo = values.EventType == NotificationType.OtherInfo;

            var sections = new Dictionary<String, Boolean>(StringComparer.Ordinal)
            {
                ["MostrarCambioEstado"] = !isComment && !isOtherInfo,
                ["MostrarEstadoUnico"] = isComment || isOtherInfo,
                ["MostrarComentario"] = isComment
            };

            var replacements = new Dictionary<String, String>(StringComparer.Ordinal)
            {
                ["ClienteNombre"] = Encode(Or(values.ClientName, DefaultClientName)),
                ["NumeroGuia"] = Encode(Or(values.DocumentNumber, EmptyValue)),
                // Textos fijos del propio código (no vienen de la notificación): no necesitan codificarse y se conservan con sus tildes.
                ["TextoIntroduccion"] = isComment ? IntroComment : isOtherInfo ? IntroOtherInfo : IntroChangeState,
                ["Origen"] = Encode(Or(values.Origin, EmptyValue)),
                ["Destino"] = Encode(Or(values.Destination, EmptyValue)),
                ["EstadoAnterior"] = Encode(Or(values.PreviousStatus, EmptyValue)),
                ["NuevoEstado"] = Encode(Or(values.NewStatus, EmptyValue)),
                ["Comentario"] = Encode(Or(values.Comment, EmptyValue)),
                ["FechaComentario"] = Encode(FormatCommentDate(values.CommentDate)),
                ["FechaHora"] = Encode(FormatDate(values.UpdatedAt)),
                ["UrlConexion360"] = BuildUrlHtml(values.Conexion360Url),
                ["LogoContentId"] = LogoContentId
            };

            // 1) Secciones opcionales (se quitan las que no aplican al tipo); 2) sustitución de {{Campos}} en una sola pasada.
            String template = SectionPattern.Replace(Template.Value, match =>
                sections.TryGetValue(match.Groups[1].Value, out Boolean visible) && visible ? match.Groups[2].Value : String.Empty);

            return TokenPattern.Replace(template, match =>
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
