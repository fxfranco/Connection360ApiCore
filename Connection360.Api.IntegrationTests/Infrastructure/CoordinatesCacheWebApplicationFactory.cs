using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Connection360.Infrastructure.Messaging;
using Microsoft.Extensions.Hosting;

namespace Connection360.Api.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Arranca la aplicación real (Program.cs, con su AddInfrastructure) y deja la composición real de
    /// IExternalApiOpenStreetMap (decorador + caché + adaptador), cambiando SOLO el transporte HTTP del cliente
    /// "OPENSTREETMAP" por <see cref="FakeNominatimHandler"/>. Así se prueba la caché tal como la ensambla el
    /// composition root, sin llamar al Nominatim real.
    ///
    /// Los parámetros de la API externa y de la caché se fijan aquí con UseSetting, de modo que las pruebas no
    /// dependan de lo que haya en los appsettings.
    ///
    /// Las pruebas que necesitan otra configuración (caché deshabilitada, otro tope de memoria...) crean su PROPIA instancia con
    /// <see cref="With"/> en vez de usar WithWebHostBuilder: los hosts hijos quedan registrados en la factory padre y ésta vuelve a
    /// liberarlos al terminar la clase, lo que provocaba "The CancellationTokenSource has been disposed" al detener los servicios
    /// hospedados (BatchingTelemetryWriter) y, según el orden, dejaba el host compartido inutilizable para las demás pruebas.
    /// Cada instancia independiente tiene además su propio <see cref="Nominatim"/>, con sus propios contadores.
    /// </summary>
    public class CoordinatesCacheWebApplicationFactory : CustomWebApplicationFactory
    {
        private readonly IReadOnlyDictionary<String, String> _settingOverrides;

        /// <summary>Constructor que usa xUnit para el fixture compartido de la clase (configuración por defecto de la prueba).</summary>
        public CoordinatesCacheWebApplicationFactory() : this(new Dictionary<String, String>())
        {
        }

        // Interno a propósito: xUnit exige que el fixture tenga un único constructor público.
        internal CoordinatesCacheWebApplicationFactory(IReadOnlyDictionary<String, String> settingOverrides)
        {
            _settingOverrides = settingOverrides;
        }

        /// <summary>Crea una factory independiente con los valores de configuración indicados (se aplican después de los de la prueba).</summary>
        public static CoordinatesCacheWebApplicationFactory With(String key, String value) =>
            new(new Dictionary<String, String> { [key] = value });

        public FakeNominatimHandler Nominatim { get; } = new();

        /// <summary>Reenvía cada petición al fake compartido sin liberarlo cuando HttpClientFactory elimina el handler.</summary>
        private sealed class ForwardingHandler(FakeNominatimHandler target) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                target.SendForwardedAsync(request, cancellationToken);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.UseSetting("ExternalApi:Apis:OPENSTREETMAP:BaseUrl", "https://nominatim.test");
            builder.UseSetting("ExternalApi:Apis:OPENSTREETMAP:DataEndpoint", "/search?q=");
            builder.UseSetting("ExternalApi:Apis:OPENSTREETMAP:TimeoutSeconds", "30");
            builder.UseSetting("CoordinatesCache:Enabled", "true");

            // La caché no depende de la observabilidad: se desactiva para que estas pruebas no escriban en el MongoDB real que haya en el
            // appsettings.Development.json local (y para no esperar el vaciado/apagado de sus escritores en segundo plano).
            builder.UseSetting("Observability:Enabled", "false");

            foreach ((String key, String value) in _settingOverrides)
            {
                builder.UseSetting(key, value);
            }

            builder.ConfigureTestServices(services =>
            {
                // Sin OutboxPublisherWorker: usaría el PostgreSQL/Kafka reales de la configuración local y la caché no los necesita.
                // (Solo se quita ese; los demás servicios hospedados, como la validación de opciones al arrancar, se conservan.)
                foreach (ServiceDescriptor worker in services
                    .Where(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(OutboxPublisherWorker))
                    .ToList())
                {
                    services.Remove(worker);
                }

                // Se entrega un reenviador desechable y NO el fake compartido: HttpClientFactory elimina los handlers que crea (al expirar o
                // al liberar el host), y las pruebas con WithWebHostBuilder crean y liberan hosts hijos que comparten este mismo fake.
                services.AddHttpClient("OPENSTREETMAP").ConfigurePrimaryHttpMessageHandler(() => new ForwardingHandler(Nominatim));
            });
        }
    }
}
