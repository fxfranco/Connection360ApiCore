using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

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
    /// </summary>
    public class CoordinatesCacheWebApplicationFactory : CustomWebApplicationFactory
    {
        public FakeNominatimHandler Nominatim { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.UseSetting("ExternalApi:Apis:OPENSTREETMAP:BaseUrl", "https://nominatim.test");
            builder.UseSetting("ExternalApi:Apis:OPENSTREETMAP:DataEndpoint", "/search?q=");
            builder.UseSetting("ExternalApi:Apis:OPENSTREETMAP:TimeoutSeconds", "30");
            builder.UseSetting("CoordinatesCache:Enabled", "true");

            builder.ConfigureTestServices(services =>
            {
                services.AddHttpClient("OPENSTREETMAP").ConfigurePrimaryHttpMessageHandler(() => Nominatim);
            });
        }
    }
}
