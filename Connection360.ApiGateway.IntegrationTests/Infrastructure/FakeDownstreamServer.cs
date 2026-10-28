using System.Net;
using System.Text;

namespace Connection360.ApiGateway.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Servidor HTTP mínimo (System.Net.HttpListener) usado como "microservicio downstream" falso
    /// para probar de punta a punta que YARP (MapReverseProxy) realmente enruta las peticiones
    /// recibidas por el Gateway hacia el destino configurado, incluyendo las transformaciones
    /// de headers definidas en appsettings (X-Forwarded-By).
    /// </summary>
    public sealed class FakeDownstreamServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _cts = new();
        private Task? _listenLoop;

        public String BaseAddress { get; }
        public String? LastReceivedPath { get; private set; }
        public String? LastReceivedForwardedByHeader { get; private set; }
        public String ResponseBody { get; set; } = "{\"ok\":true}";
        public Int32 ResponseStatusCode { get; set; } = 200;

        public FakeDownstreamServer()
        {
            Int32 port = GetFreeTcpPort();
            BaseAddress = $"http://127.0.0.1:{port}/";

            _listener = new HttpListener();
            _listener.Prefixes.Add(BaseAddress);
            _listener.Start();
        }

        public void Start()
        {
            _listenLoop = Task.Run(async () =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    try
                    {
                        HttpListenerContext context = await _listener.GetContextAsync();
                        LastReceivedPath = context.Request.Url?.PathAndQuery;
                        LastReceivedForwardedByHeader = context.Request.Headers["X-Forwarded-By"];

                        context.Response.StatusCode = ResponseStatusCode;
                        Byte[] buffer = Encoding.UTF8.GetBytes(ResponseBody);
                        context.Response.ContentType = "application/json";
                        context.Response.ContentLength64 = buffer.Length;
                        await context.Response.OutputStream.WriteAsync(buffer);
                        context.Response.Close();
                    }
                    catch (HttpListenerException)
                    {
                        break; // listener detenido
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                }
            });
        }

        private static Int32 GetFreeTcpPort()
        {
            var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            Int32 port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Stop();
            _listener.Close();
            _cts.Dispose();
        }
    }
}
