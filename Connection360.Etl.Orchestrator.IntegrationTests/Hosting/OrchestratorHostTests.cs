using Connection360.Etl.Orchestrator.Configuration;
using Connection360.Etl.Orchestrator.Execution;
using Connection360.Etl.Orchestrator.Hosting;
using Connection360.Etl.Orchestrator.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Xunit;

namespace Connection360.Etl.Orchestrator.IntegrationTests.Hosting
{
    /// <summary>
    /// Pruebas de integración del host REAL del orquestador (Program.cs): carga de appsettings por
    /// entorno, binding de configuración, registro en DI, logging y arranque/apagado del servicio
    /// hospedado. Solo se sustituye el lanzador de procesos por un fake.
    /// </summary>
    public class OrchestratorHostTests
    {
        private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

        private static Dictionary<String, String?> DisabledJob() => new() { ["Orchestrator:CronJobs:0:Enabled"] = "false" };

        // ---------- DI ----------

        [Fact]
        public void Host_Arranca_RegistraElServicioHospedadoDelOrquestador()
        {
            using var factory = new OrchestratorHostFactory(settings: DisabledJob(), runner: FakeProcessRunner.Succeeding());

            var hosted = factory.Services.GetServices<IHostedService>();

            hosted.Should().ContainSingle(s => s is OrchestratorHostedService);
        }

        [Fact]
        public void Host_SinSustituciones_ResuelveElLanzadorRealDeProcesosComoSingleton()
        {
            using var factory = new OrchestratorHostFactory(settings: DisabledJob());

            var first = factory.Services.GetRequiredService<IExternalProcessRunner>();
            var second = factory.Services.GetRequiredService<IExternalProcessRunner>();

            first.Should().BeOfType<ExternalProcessRunner>();
            second.Should().BeSameAs(first);
        }

        // ---------- Configuración por entorno ----------

        [Fact]
        public void Host_EntornoDevelopment_BindeaLaSeccionOrchestratorConAppsettingsDevelopment()
        {
            using var factory = new OrchestratorHostFactory("Development", DisabledJob(), FakeProcessRunner.Succeeding());

            var settings = factory.Services.GetRequiredService<IOptions<OrchestratorSettings>>().Value;

            settings.CronJobs.Should().HaveCount(1);
            var job = settings.CronJobs[0];
            job.Name.Should().Be("Connection360.Etl.App");
            job.Enabled.Should().BeFalse("la prueba lo deshabilita con una variable de configuración");
            job.CronExpression.Should().Be("*/1 * * * *");
            job.TimeZoneId.Should().Be("America/Bogota");
            job.ExecutablePath.Should().Be("dotnet");
            job.Arguments.Should().Be("Connection360.Etl.App.dll");
            job.WorkingDirectory.Should().Be("../../../../Connection360.Etl.App/bin/Debug/net10.0");
            job.TimeoutMinutes.Should().Be(15);
        }

        [Fact]
        public void Host_EntornoProduction_UsaLaCarpetaDeTrabajoDelAppsettingsBase()
        {
            using var factory = new OrchestratorHostFactory("Production", DisabledJob(), FakeProcessRunner.Succeeding());

            var settings = factory.Services.GetRequiredService<IOptions<OrchestratorSettings>>().Value;

            settings.CronJobs.Should().ContainSingle();
            settings.CronJobs[0].WorkingDirectory.Should().Be("../Connection360.Etl.App");
            settings.CronJobs[0].Arguments.Should().Be("Connection360.Etl.App.dll");
        }

        [Fact]
        public void Host_VariablesDeConfiguracion_SobrescribenLosValoresDelAppsettings()
        {
            var overrides = new Dictionary<String, String?>
            {
                ["Orchestrator:CronJobs:0:Enabled"] = "false",
                ["Orchestrator:CronJobs:0:Name"] = "Otro",
                ["Orchestrator:CronJobs:0:CronExpression"] = "5 4 * * 1",
                ["Orchestrator:CronJobs:0:TimeoutMinutes"] = "3",
                ["Orchestrator:CronJobs:1:Name"] = "Segundo",
                ["Orchestrator:CronJobs:1:Enabled"] = "false",
                ["Orchestrator:CronJobs:1:CronExpression"] = "0 0 * * *",
            };
            using var factory = new OrchestratorHostFactory(settings: overrides, runner: FakeProcessRunner.Succeeding());

            var settings = factory.Services.GetRequiredService<IOptions<OrchestratorSettings>>().Value;

            settings.CronJobs.Should().HaveCount(2);
            settings.CronJobs[0].Name.Should().Be("Otro");
            settings.CronJobs[0].CronExpression.Should().Be("5 4 * * 1");
            settings.CronJobs[0].TimeoutMinutes.Should().Be(3);
            settings.CronJobs[1].Name.Should().Be("Segundo");
            settings.CronJobs[1].ExecutablePath.Should().Be("dotnet", "conserva el valor por defecto de la clase de configuración");
        }

        // ---------- Program: entorno y logging ----------

        [Theory]
        [InlineData("Development")]
        [InlineData("Production")]
        public async Task Host_Arranca_RegistraElEntornoActivo(String environment)
        {
            using var factory = new OrchestratorHostFactory(environment, DisabledJob(), FakeProcessRunner.Succeeding());

            _ = factory.Services;

            var entry = await factory.Logs.WaitForAsync(e => e.Message.StartsWith("Entorno activo"), Wait);
            entry.Level.Should().Be(LogLevel.Information);
            entry.Message.Should().Be($"Entorno activo (DOTNET_ENVIRONMENT): {environment}");
            factory.Services.GetRequiredService<IHostEnvironment>().EnvironmentName.Should().Be(environment);
        }

        [Fact]
        public void Host_Logging_UsaConsolaSimpleDeUnaSolaLineaConTimestamp()
        {
            using var factory = new OrchestratorHostFactory(settings: DisabledJob(), runner: FakeProcessRunner.Succeeding());

            var formatter = factory.Services.GetRequiredService<IOptionsMonitor<SimpleConsoleFormatterOptions>>().CurrentValue;
            var providers = factory.Services.GetServices<ILoggerProvider>().Select(p => p.GetType().Name).ToList();

            formatter.SingleLine.Should().BeTrue();
            formatter.TimestampFormat.Should().Be("yyyy-MM-dd HH:mm:ss ");
            providers.Should().Contain("ConsoleLoggerProvider");
            providers.Should().NotContain("DebugLoggerProvider", "Program limpia los proveedores por defecto");
        }

        // ---------- Servicio hospedado dentro del host real ----------

        [Fact]
        public async Task Host_SinTrabajosHabilitados_AdviertePorLogYQuedaEnEjecucion()
        {
            using var factory = new OrchestratorHostFactory(settings: DisabledJob(), runner: FakeProcessRunner.Succeeding());

            _ = factory.Services;

            var warning = await factory.Logs.WaitForAsync(e => e.Message.Contains("No hay trabajos habilitados"), Wait);
            warning.Level.Should().Be(LogLevel.Warning);
            warning.Category.Should().Be(typeof(OrchestratorHostedService).FullName);
        }

        [Fact]
        public async Task Host_TrabajoHabilitado_ProgramaLaProximaEjecucionConLaConfiguracionBindeada()
        {
            var runner = FakeProcessRunner.Succeeding();
            var settings = new Dictionary<String, String?>
            {
                ["Orchestrator:CronJobs:0:Name"] = "etl-integracion",
                ["Orchestrator:CronJobs:0:TimeZoneId"] = "UTC",
                ["Orchestrator:CronJobs:0:CronExpression"] = "0 0 1 * *",
            };
            using var factory = new OrchestratorHostFactory(settings: settings, runner: runner);

            _ = factory.Services;

            await factory.Logs.WaitForAsync(e => e.Message == "Orquestador iniciado con 1 trabajo(s) programado(s).", Wait);
            await factory.Logs.WaitForAsync(e => e.Message.StartsWith("[etl-integracion] Programado con expresión cron '0 0 1 * *' (zona horaria: UTC)"), Wait);
            var next = await factory.Logs.WaitForAsync(e => e.Message.StartsWith("[etl-integracion] Próxima ejecución"), Wait);
            next.Category.Should().Be("CronJobRunnerLoop.etl-integracion");
            runner.Calls.Should().BeEmpty("la próxima ejecución es el día 1 del mes siguiente");
        }

        [Fact]
        public async Task Host_ConfiguracionInvalidaDeUnTrabajo_ElServicioFallaYElErrorQuedaRegistrado()
        {
            var settings = new Dictionary<String, String?>
            {
                ["Orchestrator:CronJobs:0:Arguments"] = "",
            };
            using var factory = new OrchestratorHostFactory(settings: settings, runner: FakeProcessRunner.Succeeding());

            try
            {
                _ = factory.Services;
            }
            catch (Exception)
            {
                // Según el comportamiento de BackgroundService ante excepciones, el arranque puede
                // propagar el fallo o dejar que el host se detenga solo: en ambos casos el error debe quedar en el log.
            }

            var critical = await factory.Logs.WaitForAsync(e => e.Level == LogLevel.Critical && e.Exception is InvalidOperationException, Wait);
            critical.Exception!.Message.Should().Contain("no tiene 'Arguments' configurado");
        }

        // ---------- Programación real (espera hasta el próximo minuto calendario) ----------

        [Fact]
        public async Task Host_TrabajoCadaMinuto_EjecutaElTrabajoAlLlegarLaHoraYLoRegistra()
        {
            // El bucle usa el reloj real: la primera ejecución llega en el próximo minuto calendario (<= 60 s).
            var runner = FakeProcessRunner.Succeeding();
            var settings = new Dictionary<String, String?>
            {
                ["Orchestrator:CronJobs:0:Name"] = "etl-cada-minuto",
                ["Orchestrator:CronJobs:0:TimeZoneId"] = "UTC",
                ["Orchestrator:CronJobs:0:CronExpression"] = "* * * * *",
            };
            using var factory = new OrchestratorHostFactory(settings: settings, runner: runner);

            _ = factory.Services;

            await runner.FirstCall.WaitAsync(TimeSpan.FromSeconds(100));

            var job = runner.Calls[0];
            job.Name.Should().Be("etl-cada-minuto");
            job.ExecutablePath.Should().Be("dotnet");
            job.Arguments.Should().Be("Connection360.Etl.App.dll");
            job.TimeoutMinutes.Should().Be(15);
            await factory.Logs.WaitForAsync(e => e.Message == "[etl-cada-minuto] Iniciando ejecución programada...", Wait);
            await factory.Logs.WaitForAsync(e => e.Message.StartsWith("[etl-cada-minuto] Ejecución finalizada correctamente"), Wait);
        }
    }
}
