using Microsoft.Extensions.Configuration;

namespace Connection360.Etl.App.IntegrationTests.Infrastructure
{
    /// <summary>Carga el appsettings.json REAL de Connection360.Etl.App (copiado junto a las pruebas) más sobrescrituras en memoria.</summary>
    public static class AppSettingsLoader
    {
        /// <summary>Hosts ficticios ".test" por API, para que ninguna prueba toque una URL real (localhost:443xx).</summary>
        public static readonly IReadOnlyDictionary<String, String> ApiHosts = new Dictionary<String, String>
        {
            ["BPMS"] = "bpms.test",
            ["DATALOGS"] = "datalogs.test",
            ["SIM"] = "sim.test",
            ["OPENCOMEX"] = "opencomex.test",
            ["ASISCOMEX"] = "asiscomex.test",
            ["SYSTEMCARRIER"] = "systemcarrier.test",
        };

        public static String AppSettingsPath => Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        public static IConfigurationRoot Load(IEnumerable<KeyValuePair<String, String?>>? overrides = null, Boolean useStubHosts = true)
        {
            var builder = new ConfigurationBuilder().AddJsonFile(AppSettingsPath, optional: false, reloadOnChange: false);

            if (useStubHosts)
            {
                builder.AddInMemoryCollection(ApiHosts.Select(kv =>
                    new KeyValuePair<String, String?>($"ExternalApi:Apis:{kv.Key}:BaseUrl", $"http://{kv.Value}")));
            }

            if (overrides is not null)
                builder.AddInMemoryCollection(overrides);

            return builder.Build();
        }
    }
}
