using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Connection360Notification.Infrastructure.Sms
{
    /// <summary>
    /// Implementación de <see cref="ISmsSender"/> con la API de SMS transaccional de Brevo
    /// (<c>POST {BaseUrl}transactionalSMS/send</c>, encabezado <c>api-key</c>, respuesta 201 con <c>messageId</c>).
    /// El texto lleva tildes, por eso se envía con <c>unicodeEnabled = true</c> (así Brevo lo codifica como Unicode y no
    /// como GSM-7, que desfiguraría las letras acentuadas). Ni la API key ni el celular se incluyen en los mensajes de error.
    /// </summary>
    public sealed class BrevoSmsSender : ISmsSender
    {
        /// <summary>Ruta del endpoint de envío, relativa a <see cref="BrevoSmsSettings.BaseUrl"/>.</summary>
        public const String SendPath = "transactionalSMS/send";

        /// <summary>Tipo de mensaje enviado a Brevo.</summary>
        public const String TransactionalType = "transactional";

        private const Int32 MaxErrorLength = 300;

        private readonly HttpClient _httpClient;
        private readonly BrevoSmsSettings _settings;

        public BrevoSmsSender(HttpClient httpClient, IOptions<BrevoSmsSettings> settings)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
        }

        /// <summary>Arma la solicitud HTTP (URL, encabezados y cuerpo JSON) sin enviarla.</summary>
        public HttpRequestMessage CreateRequest(SmsMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);

            String baseUrl = String.IsNullOrWhiteSpace(_settings.BaseUrl) ? BrevoSmsSettings.DefaultBaseUrl : _settings.BaseUrl.Trim();
            if (!baseUrl.EndsWith('/'))
            {
                baseUrl += "/";
            }

            var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(baseUrl), SendPath));
            request.Headers.Add("api-key", _settings.ApiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            String body = JsonSerializer.Serialize(new Dictionary<String, Object>
            {
                ["sender"] = _settings.Sender,
                ["recipient"] = message.Recipient,
                ["content"] = message.Content,
                ["type"] = TransactionalType,
                ["unicodeEnabled"] = true
            });
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            return request;
        }

        public async Task<String?> SendAsync(SmsMessage message, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(message);

            if (String.IsNullOrWhiteSpace(_settings.ApiKey) || String.IsNullOrWhiteSpace(_settings.Sender))
            {
                throw new InvalidOperationException(
                    $"El envío de SMS no está configurado: falta ApiKey o Sender en la sección '{BrevoSmsSettings.SectionName}'.");
            }

            using HttpRequestMessage request = CreateRequest(message);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _settings.TimeoutSeconds)));

            try
            {
                using HttpResponseMessage response = await _httpClient.SendAsync(request, timeout.Token);
                String responseBody = await response.Content.ReadAsStringAsync(timeout.Token);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(
                        $"Brevo rechazó el envío del SMS (HTTP {(Int32)response.StatusCode}): {Truncate(DescribeError(responseBody))}",
                        inner: null,
                        statusCode: response.StatusCode);
                }

                return ReadMessageId(responseBody);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"Brevo no respondió en {Math.Max(1, _settings.TimeoutSeconds)} segundos al enviar el SMS.");
            }
        }

        /// <summary>Identificador (<c>messageId</c>) de la respuesta 201 de Brevo; <c>null</c> si no viene o no es JSON.</summary>
        public static String? ReadMessageId(String responseBody)
        {
            if (String.IsNullOrWhiteSpace(responseBody))
            {
                return null;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(responseBody);
                if (document.RootElement.ValueKind == JsonValueKind.Object
                    && document.RootElement.TryGetProperty("messageId", out JsonElement id))
                {
                    return id.ValueKind == JsonValueKind.String ? id.GetString() : id.ToString();
                }
            }
            catch (JsonException)
            {
            }

            return null;
        }

        /// <summary>Texto del error de Brevo (<c>code</c> y <c>message</c>); si no es JSON, el cuerpo tal cual.</summary>
        public static String DescribeError(String responseBody)
        {
            if (String.IsNullOrWhiteSpace(responseBody))
            {
                return "sin detalle";
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(responseBody);
                if (document.RootElement.ValueKind == JsonValueKind.Object)
                {
                    String? code = document.RootElement.TryGetProperty("code", out JsonElement c) ? c.ToString() : null;
                    String? text = document.RootElement.TryGetProperty("message", out JsonElement m) ? m.ToString() : null;
                    if (!String.IsNullOrWhiteSpace(code) || !String.IsNullOrWhiteSpace(text))
                    {
                        return String.Join(" - ", new[] { code, text }.Where(v => !String.IsNullOrWhiteSpace(v)));
                    }
                }
            }
            catch (JsonException)
            {
            }

            return responseBody;
        }

        private static String Truncate(String value) =>
            value.Length <= MaxErrorLength ? value : value.Substring(0, MaxErrorLength) + "...";
    }
}
