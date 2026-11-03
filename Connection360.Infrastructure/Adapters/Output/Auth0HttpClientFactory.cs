using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace Connection360.Infrastructure.Adapters.Output
{
    /// <summary>
    /// <see cref="HttpClient"/> compartido para hablar con Auth0 (token M2M y Management API) que prueba primero las direcciones IPv4 y
    /// limita cada intento de conexión.
    ///
    /// Motivo: en redes donde el DNS devuelve direcciones IPv6 pero no hay ruta IPv6 real, .NET intenta las direcciones una tras otra y cada
    /// una espera ~21 s a que venza su timeout antes de pasar a la siguiente (con 2 direcciones IPv6, ~43 s). El cliente de la Management API
    /// tiene un timeout total de 30 s, así que se cancelaba ("A task was canceled") ANTES de llegar a IPv4 y la petición nunca salía hacia Auth0.
    /// Con IPv4 primero, y como máximo <see cref="PerAddressTimeout"/> por dirección, la conexión se establece en milisegundos cuando IPv4
    /// funciona y sigue sin romperse en redes solo IPv6 (se usan las direcciones IPv6 después de las IPv4).
    /// </summary>
    public static class Auth0HttpClientFactory
    {
        /// <summary>Tiempo máximo de cada intento de conexión a una dirección (para que una ruta rota no consuma el timeout total).</summary>
        public static readonly TimeSpan PerAddressTimeout = TimeSpan.FromSeconds(5);

        private static readonly Lazy<HttpClient> SharedClient = new(CreateClient);

        /// <summary>Cliente compartido por proceso (no se libera): reutiliza conexiones y las renueva cada 5 minutos para respetar cambios de DNS.</summary>
        public static HttpClient Shared => SharedClient.Value;

        private static HttpClient CreateClient()
        {
            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                ConnectCallback = (context, cancellationToken) => ConnectAsync(context.DnsEndPoint, cancellationToken)
            };

            return new HttpClient(handler);
        }

        /// <summary>Direcciones en el orden de intento: primero IPv4 y después IPv6 (sin repetidas, respetando el orden del DNS dentro de cada familia).</summary>
        public static IReadOnlyList<IPAddress> OrderIPv4First(IEnumerable<IPAddress> addresses)
        {
            ArgumentNullException.ThrowIfNull(addresses);

            return addresses
                .Distinct()
                .OrderBy(address => address.AddressFamily == AddressFamily.InterNetwork ? 0 : 1)
                .ToList();
        }

        /// <summary>
        /// Abre la conexión TCP hacia el host (el handler aplica TLS sobre el flujo devuelto). Prueba cada dirección en orden con
        /// <see cref="PerAddressTimeout"/>; si todas fallan lanza <see cref="HttpRequestException"/> con el último error.
        /// </summary>
        public static async ValueTask<Stream> ConnectAsync(DnsEndPoint endPoint, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(endPoint);

            IPAddress[] resolved = await Dns.GetHostAddressesAsync(endPoint.Host, cancellationToken).ConfigureAwait(false);
            Exception? lastError = null;

            foreach (IPAddress address in OrderIPv4First(resolved))
            {
                using CancellationTokenSource attempt = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                attempt.CancelAfter(PerAddressTimeout);

                var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                try
                {
                    await socket.ConnectAsync(new IPEndPoint(address, endPoint.Port), attempt.Token).ConfigureAwait(false);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    // Timeout del intento o error de red: se descarta el socket y se prueba la siguiente dirección.
                    socket.Dispose();
                    lastError = ex;
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }

            throw new HttpRequestException($"No fue posible conectar con {endPoint.Host}:{endPoint.Port} por ninguna de sus direcciones.", lastError);
        }
    }
}
