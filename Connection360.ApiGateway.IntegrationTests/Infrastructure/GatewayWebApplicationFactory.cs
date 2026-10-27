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
        /// <summary>
        /// Valores de prueba para las secciones que los appsettings versionados dejan en blanco a propósito (Jwt, Cors) para no
        /// versionar datos sensibles. Sin ellos el gateway no arranca bien: un Jwt:Roles vacío lanza ArgumentOutOfRangeException
        /// (IDX10103) al crear las opciones de JwtBearer y una clave vacía impide firmar tokens. Son valores ficticios, no reales.
        /// </summary>
        private static readonly Dictionary<String, String?> BaseTestSettings = new()
        {
            ["Jwt:Issuer"] = "https://issuer.tests.local/",
            ["Jwt:Audience"] = "connection360-gateway-tests",
            ["Jwt:Secret"] = new String('k', 64),
            ["Jwt:AccessTokenMinutes"] = "15",
            ["Jwt:RefreshTokenDays"] = "7",
            ["Jwt:Roles"] = "https://conexion360.space/roles",
            ["Cors:AllowedOrigins:0"] = "https://miapp.miempresa.com"
        };

        private readonly Dictionary<String, String?> _configOverrides;

        public GatewayWebApplicationFactory(Dictionary<String, String?>? configOverrides = null)
        {
            _configOverrides = configOverrides ?? new Dictionary<String, String?>();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            // UseSetting (configuración del host) sí es visible para el código de Program.cs que lee builder.Configuration al
            // construir el contenedor (autenticación JWT, CORS). Primero la base y después los valores de cada prueba, para que estos ganen.
            foreach (KeyValuePair<String, String?> setting in BaseTestSettings)
            {
                builder.UseSetting(setting.Key, setting.Value);
            }

            foreach (KeyValuePair<String, String?> setting in _configOverrides)
            {
                builder.UseSetting(setting.Key, setting.Value);
            }

            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(_configOverrides);
            });
        }
    }
}
