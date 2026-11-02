using System;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Connection360.Etl.Application.UseCases
{
    /// <summary>
    /// Convierte el error capturado al consultar las APIs externas en un texto apto para notificar a los
    /// administradores: indica el MOTIVO del fallo sin exponer datos sensibles (direcciones y URL de los servicios,
    /// credenciales, tokens, cadenas de conexión, correos, rutas de archivos, etc.).
    /// <para>
    /// Primero se traducen los fallos conocidos a una descripción fija (respuesta con error HTTP, sin conexión, tiempo
    /// de espera agotado, respuesta con formato inesperado, error de seguridad). Para cualquier otro error se usa su
    /// mensaje, enmascarando lo sensible, sin saltos de línea y recortado a <see cref="MaxLength"/> caracteres. Nunca se
    /// incluye el stack trace.
    /// </para>
    /// </summary>
    public static class EtlErrorSanitizer
    {
        /// <summary>Longitud máxima del texto devuelto (para que la alerta sea legible y quepa en un SMS razonable).</summary>
        public const Int32 MaxLength = 200;

        public const String HttpStatusReasonPrefix = "El API externo respondió con status";
        public const String ConnectionFailedReason = "No fue posible establecer conexión con el API externo";
        public const String TimeoutReason = "Se agotó el tiempo de espera al consultar el API externo";
        public const String InvalidResponseReason = "La respuesta del API externo no tiene el formato esperado";
        public const String SecureConnectionReason = "No fue posible establecer la conexión segura con el API externo";
        public const String UnknownReason = "Error no identificado al consultar los datos de las APIs externas";

        private const String Masked = "[oculto]";

        /// <summary>
        /// Tiempo máximo de cada expresión regular (evita el retroceso excesivo, ReDoS, con textos de error muy largos).
        /// </summary>
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

        private const RegexOptions Options = RegexOptions.Compiled | RegexOptions.CultureInvariant;

        private static readonly Regex HttpStatusMessage = new(@"\bstatus\s+(\d{3})\b", Options | RegexOptions.IgnoreCase, RegexTimeout);

        private static readonly Regex Credential = new(
            @"\b(?:Bearer|Basic)\s+[A-Za-z0-9._~+/=\-]+|\beyJ[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]*",
            Options | RegexOptions.IgnoreCase, RegexTimeout);

        private static readonly Regex KeyValueSecret = new(
            @"\b(password|passwd|pwd|secret|token|api[-_ ]?key|access[-_ ]?key|client[-_ ]?secret|authorization|connection\s*string|user\s*id|uid|server|host|data\s*source|database|initial\s*catalog)\s*[=:]\s*(?:""[^""]*""|'[^']*'|[^;,\s]+)",
            Options | RegexOptions.IgnoreCase, RegexTimeout);

        private static readonly Regex Url = new(@"\b[a-z][a-z0-9+.\-]*://[^\s'""<>]+", Options | RegexOptions.IgnoreCase, RegexTimeout);

        private static readonly Regex Email = new(@"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}", Options, RegexTimeout);

        private static readonly Regex IpAddress = new(@"\b\d{1,3}(?:\.\d{1,3}){3}(?::\d{1,5})?\b", Options, RegexTimeout);

        // Nombres de host con un sufijo de dominio común (en minúsculas, para no confundirlos con nombres de tipo como System.Net.Http).
        private static readonly Regex HostName = new(
            @"\b(?:[A-Za-z0-9\-]+\.)+(?:com|net|org|co|io|dev|app|cloud|local|internal|lan|corp|gov|edu|info|biz|us|es|mx|br|ar|cl|pe|ec)\b(?::\d{1,5})?",
            Options, RegexTimeout);

        private static readonly Regex WindowsPath = new(@"(?:\b[A-Za-z]:\\|\\\\)[^\s'""<>|?*]+", Options, RegexTimeout);

        private static readonly Regex UnixPath = new(@"(?<![\w:/])/(?:[\w.\-]+/)+[\w.\-]*", Options, RegexTimeout);

        private static readonly Regex OpaqueToken = new(@"\b[A-Za-z0-9_\-]{32,}\b", Options, RegexTimeout);

        private static readonly Regex Whitespace = new(@"\s+", Options, RegexTimeout);

        /// <summary>
        /// Devuelve el motivo del error, sanitizado y listo para incluirlo entre comillas simples en el mensaje de la alerta.
        /// </summary>
        public static String Sanitize(Exception? error)
        {
            if (error is null)
            {
                return UnknownReason;
            }

            // Del error más externo al más interno: el primero que se reconoce define el motivo.
            for (Exception? current = error; current is not null; current = current.InnerException)
            {
                String? known = DescribeKnownFailure(current);
                if (known is not null)
                {
                    return known;
                }
            }

            String text = SanitizeText(error.Message);
            return text.Length > 0 ? text : $"{UnknownReason} ({error.GetType().Name})";
        }

        /// <summary>
        /// Enmascara lo sensible de un texto libre, lo deja en una sola línea, cambia las comillas simples por dobles (el
        /// texto se muestra entre comillas simples) y lo recorta a <see cref="MaxLength"/>.
        /// </summary>
        public static String SanitizeText(String? text)
        {
            if (String.IsNullOrWhiteSpace(text))
            {
                return String.Empty;
            }

            try
            {
                String result = Credential.Replace(text, Masked);
                result = KeyValueSecret.Replace(result, "$1=" + Masked);
                result = Url.Replace(result, "[url]");
                result = Email.Replace(result, "[correo]");
                result = IpAddress.Replace(result, "[ip]");
                result = HostName.Replace(result, "[servidor]");
                result = WindowsPath.Replace(result, "[ruta]");
                result = UnixPath.Replace(result, "[ruta]");
                result = OpaqueToken.Replace(result, Masked);
                result = Whitespace.Replace(result, " ").Trim().Replace('\'', '"');

                return Truncate(result);
            }
            catch (RegexMatchTimeoutException)
            {
                // Un texto patológico no debe impedir la alerta: se descarta su contenido en vez de arriesgarse a mostrarlo sin sanear.
                return String.Empty;
            }
        }

        private static String? DescribeKnownFailure(Exception error)
        {
            switch (error)
            {
                case HttpRequestException http when http.StatusCode is not null:
                    return $"{HttpStatusReasonPrefix} {(Int32)http.StatusCode.Value}";

                case HttpRequestException http:
                    // El gateway lanza "El API externo respondió con status NNN": ese motivo ya es seguro y se conserva.
                    Match status = HttpStatusMessage.Match(http.Message ?? String.Empty);
                    return status.Success
                        ? $"{HttpStatusReasonPrefix} {status.Groups[1].Value}"
                        : ConnectionFailedReason;

                case SocketException:
                    return ConnectionFailedReason;

                case TimeoutException:
                case OperationCanceledException:
                    // Solo llega aquí cuando la cancelación no la pidió el proceso (p. ej. el HttpClient agotó su tiempo).
                    return TimeoutReason;

                case JsonException:
                    return InvalidResponseReason;

                case AuthenticationException:
                    return SecureConnectionReason;

                default:
                    return null;
            }
        }

        private static String Truncate(String value)
        {
            if (value.Length <= MaxLength)
            {
                return value;
            }

            return value.Substring(0, MaxLength - 3).TrimEnd() + "...";
        }
    }
}
