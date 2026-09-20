using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Connection360.ApiGateway.IntegrationTests.Infrastructure
{
    /// <summary>
    /// WebApplicationFactory que arranca el Gateway real (Program.cs, YARP incluido) permitiendo
    /// sobreescribir configuración puntual por prueba (p.ej. el destino del cluster de YARP,
    /// para poder apuntar a un servidor downstream falso controlado por el test).
    /// </summary>
    public class GatewayWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly Dictionary<String, String?> _configOverrides;

        public GatewayWebApplicationFactory(Dictionary<String, String?>? configOverrides = null)
        {
            _configOverrides = configOverrides ?? new Dictionary<String, String?>();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(_configOverrides);
            });
        }
    }
}
