using Connection360.Etl.Orchestrator.Execution;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Connection360.Etl.Orchestrator.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Arranca el host REAL del orquestador (Program.cs: configuración, logging, DI y servicio
    /// hospedado) sin tocar nada de producción. Se apoya en WebApplicationFactory porque este
    /// también sabe controlar hosts genéricos (Host.CreateApplicationBuilder): intercepta la
    /// construcción del host y lo arranca/detiene con el ciclo de vida de la factory.
    /// <para>
    /// El tipo genérico es <c>CronExpression</c> únicamente para localizar el ensamblado y su punto
    /// de entrada (la clase Program de los top-level statements es internal).
    /// </para>
    /// Lo único sustituido son los bordes: el lanzador de procesos (IExternalProcessRunner) por un
    /// fake, para no ejecutar nunca Connection360.Etl.App de verdad.
    /// </summary>
    public sealed class OrchestratorHostFactory : WebApplicationFactory<Scheduling.CronExpression>
    {
        private readonly String _environment;
        private readonly IReadOnlyDictionary<String, String?> _settings;
        private readonly IExternalProcessRunner? _runner;

        public OrchestratorHostFactory(
            String environment = "Development",
            IReadOnlyDictionary<String, String?>? settings = null,
            IExternalProcessRunner? runner = null)
        {
            _environment = environment;
            _settings = settings ?? new Dictionary<String, String?>();
            _runner = runner;
        }

        /// <summary>Logs de TODO el host (orquestador, bucles y el propio Program).</summary>
        public ListLoggerProvider Logs { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Los appsettings*.json del orquestador se copian junto a las pruebas.
            builder.UseContentRoot(AppContext.BaseDirectory);
            builder.UseEnvironment(_environment);

            // GenericWebHostService exige una aplicación configurada; no se usa ningún endpoint.
            builder.Configure(_ => { });

            foreach (var (key, value) in _settings)
            {
                builder.UseSetting(key, value);
            }

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ILoggerProvider>(Logs);

                if (_runner is not null)
                    services.Replace(ServiceDescriptor.Singleton(_runner));
            });
        }
    }
}
