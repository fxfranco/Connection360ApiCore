using System.Net;
using System.Text;
using System.Text.Json;

namespace Connection360.Etl.App.IntegrationTests.Infrastructure
{
    /// <summary>Petición HTTP registrada por <see cref="StubApiHandler"/>.</summary>
    public sealed record RecordedRequest(String Host, String Path, IReadOnlyDictionary<String, String> Query, String? ApiKey);

    /// <summary>
    /// Reemplazo del HttpMessageHandler primario de los HttpClient nombrados de las APIs externas:
    /// responde según el host de la petición y registra cada llamada. Nunca hay red real.
    /// </summary>
    public sealed class StubApiHandler : HttpMessageHandler
    {
        private readonly List<RecordedRequest> _requests = new();

        /// <summary>Respuestas por host (por ejemplo "bpms.test"): reciben la petición y devuelven (status, cuerpo JSON).</summary>
        public Dictionary<String, Func<RecordedRequest, (HttpStatusCode Status, String Body)>> Routes { get; } = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<RecordedRequest> Requests
        {
            get
            {
                lock (_requests)
                    return _requests.ToArray();
            }
        }

        public IEnumerable<RecordedRequest> RequestsTo(String host) =>
            Requests.Where(r => r.Host.Equals(host, StringComparison.OrdinalIgnoreCase));

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Uri uri = request.RequestUri ?? throw new InvalidOperationException("Petición sin URI.");

            var query = uri.Query.TrimStart('?')
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(pair => pair.Split('=', 2))
                .ToDictionary(p => Uri.UnescapeDataString(p[0]), p => p.Length > 1 ? Uri.UnescapeDataString(p[1]) : String.Empty);

            String? apiKey = request.Headers.TryGetValues("X-Api-Key", out var values) ? values.FirstOrDefault() : null;
            var recorded = new RecordedRequest(uri.Host, uri.AbsolutePath, query, apiKey);

            lock (_requests)
                _requests.Add(recorded);

            if (!Routes.TryGetValue(uri.Host, out var route))
                throw new InvalidOperationException($"No hay respuesta simulada para el host '{uri.Host}'.");

            var (status, body) = route(recorded);
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }

        /// <summary>Cuerpo JSON con la forma exacta que devuelven las APIs externas.</summary>
        public static String Page(IEnumerable<String> fields, params IDictionary<String, String>[] rows) =>
            JsonSerializer.Serialize(new
            {
                requestedFields = fields.ToArray(),
                missingColumns = Array.Empty<String>(),
                rows,
            });

        public static (HttpStatusCode, String) Ok(String body) => (HttpStatusCode.OK, body);
    }
}
