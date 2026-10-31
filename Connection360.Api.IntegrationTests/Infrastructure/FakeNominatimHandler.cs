using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Web;

namespace Connection360.Api.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Sustituye al servidor real de Nominatim (OpenStreetMap) en las pruebas de integración: responde con
    /// coordenadas fijas para cualquier lugar y cuenta cuántas peticiones HTTP "salieron" por lugar, que es
    /// lo que permite comprobar que la caché evita consultas repetidas al API externo.
    /// </summary>
    public sealed class FakeNominatimHandler : HttpMessageHandler
    {
        private readonly ConcurrentDictionary<String, Int32> _requestsByPlace = new(StringComparer.OrdinalIgnoreCase);
        private Int32 _totalRequests;

        /// <summary>Retardo artificial de cada respuesta (para simular un API lento y forzar concurrencia).</summary>
        public TimeSpan Latency { get; set; } = TimeSpan.Zero;

        /// <summary>Si no es null, devuelve ese status (por ejemplo 500) en vez de las coordenadas.</summary>
        public HttpStatusCode? FailWith { get; set; }

        public Int32 TotalRequests => Volatile.Read(ref _totalRequests);

        public Int32 RequestsFor(String place) => _requestsByPlace.TryGetValue(place, out Int32 count) ? count : 0;

        public void Reset()
        {
            _requestsByPlace.Clear();
            Interlocked.Exchange(ref _totalRequests, 0);
            Latency = TimeSpan.Zero;
            FailWith = null;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            String place = HttpUtility.ParseQueryString(request.RequestUri!.Query)["q"] ?? String.Empty;
            _requestsByPlace.AddOrUpdate(place, 1, (_, current) => current + 1);
            Interlocked.Increment(ref _totalRequests);

            if (Latency > TimeSpan.Zero)
            {
                await Task.Delay(Latency, cancellationToken);
            }

            if (FailWith is { } status)
            {
                return new HttpResponseMessage(status) { Content = new StringContent("error simulado") };
            }

            String json = $$"""[{ "display_name": "{{place}}, Mundo", "lat": "4.7110", "lon": "-74.0721" }]""";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }
    }
}
