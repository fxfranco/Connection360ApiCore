using Connection360.Etl.Orchestrator.Configuration;
using Connection360.Etl.Orchestrator.Hosting;
using Connection360.Etl.Orchestrator.Test.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Connection360.Etl.Orchestrator.Test.Hosting
{
    public class OrchestratorHostedServiceTests
    {
        private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

        private readonly ListLoggerProvider _logs = new();
        private readonly FakeProcessRunner _runner = FakeProcessRunner.Succeeding();

        private OrchestratorHostedService CreateService(params CronJobSettings[] jobs)
        {
            var settings = new OrchestratorSettings { CronJobs = jobs.ToList() };
            var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(_logs));
            return new OrchestratorHostedService(
                Options.Create(settings),
                _runner,
                loggerFactory,
                loggerFactory.CreateLogger<OrchestratorHostedService>());
        }

        /// <summary>
        /// Arranca el servicio y espera a que termine su ExecuteAsync: los errores de validación/armado
        /// de la programación se manifiestan ahí (BackgroundService los captura en ExecuteTask).
        /// </summary>
        private static async Task StartAndAwaitExecutionAsync(OrchestratorHostedService service)
        {
            await service.StartAsync(CancellationToken.None);
            await service.ExecuteTask!.WaitAsync(Wait);
        }

        // ---------- Sin trabajos habilitados ----------

        [Fact]
        public async Task StartAsync_SinTrabajos_AdviertePorLogYTerminaSinProgramarNada()
        {
            var service = CreateService();

            await service.StartAsync(CancellationToken.None);
            await service.ExecuteTask!;

            _logs.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning && e.Message.Contains("No hay trabajos habilitados") && e.Message.Contains("Orchestrator"));
            _runner.Calls.Should().BeEmpty();
            await service.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task StartAsync_TodosLosTrabajosDeshabilitados_NoLosValidaNiLosProgramaAunqueSeanInvalidos()
        {
            // Un trabajo deshabilitado y con campos vacíos no debe provocar error de validación.
            var disabled = new CronJobSettings { Name = "", Enabled = false, CronExpression = "no es cron" };
            var service = CreateService(disabled);

            await service.StartAsync(CancellationToken.None);
            await service.ExecuteTask!;

            _logs.Entries.Should().Contain(e => e.Level == LogLevel.Warning && e.Message.Contains("No hay trabajos habilitados"));
            _logs.Entries.Should().NotContain(e => e.Level == LogLevel.Error);
        }

        // ---------- Validación ----------

        [Theory]
        [InlineData("Name", "Un trabajo en 'Orchestrator:CronJobs' no tiene 'Name' configurado.")]
        [InlineData("CronExpression", "El trabajo 'job1' no tiene 'CronExpression' configurada.")]
        [InlineData("ExecutablePath", "El trabajo 'job1' no tiene 'ExecutablePath' configurado.")]
        [InlineData("Arguments", "El trabajo 'job1' no tiene 'Arguments' configurado.")]
        [InlineData("WorkingDirectory", "El trabajo 'job1' no tiene 'WorkingDirectory' configurado.")]
        public async Task StartAsync_TrabajoHabilitadoSinCampoObligatorio_LanzaInvalidOperationException(String missingField, String expectedMessage)
        {
            var job = JobFactory.Valid();
            switch (missingField)
            {
                case "Name": job.Name = " "; break;
                case "CronExpression": job.CronExpression = ""; break;
                case "ExecutablePath": job.ExecutablePath = " "; break;
                case "Arguments": job.Arguments = null!; break;
                case "WorkingDirectory": job.WorkingDirectory = ""; break;
            }
            var service = CreateService(job);

            Func<Task> act = () => StartAndAwaitExecutionAsync(service);

            (await act.Should().ThrowAsync<InvalidOperationException>()).WithMessage(expectedMessage);
            _runner.Calls.Should().BeEmpty();
        }

        [Fact]
        public async Task StartAsync_UnTrabajoInvalidoEntreVarios_NoArrancaNingunBucle()
        {
            var invalid = JobFactory.Valid("malo");
            invalid.Arguments = "";
            var service = CreateService(JobFactory.Valid("bueno"), invalid);

            Func<Task> act = () => StartAndAwaitExecutionAsync(service);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*'malo'*");
            _logs.Entries.Should().NotContain(e => e.Message.Contains("Programado con expresión cron"));
        }

        [Fact]
        public async Task StartAsync_ExpresionCronInvalida_LanzaFormatException()
        {
            var service = CreateService(JobFactory.Valid(cron: "esto no es cron"));

            Func<Task> act = () => StartAndAwaitExecutionAsync(service);

            await act.Should().ThrowAsync<FormatException>();
        }

        [Fact]
        public async Task StartAsync_ZonaHorariaInexistente_LanzaTimeZoneNotFoundException()
        {
            var job = JobFactory.Valid();
            job.TimeZoneId = "Zona/Que/No/Existe";
            var service = CreateService(job);

            Func<Task> act = () => StartAndAwaitExecutionAsync(service);

            await act.Should().ThrowAsync<TimeZoneNotFoundException>();
        }

        // ---------- Arranque y apagado ----------

        [Fact]
        public async Task StartAsync_TrabajoValido_ArrancaUnBucleQueProgramaLaProximaEjecucion()
        {
            var service = CreateService(JobFactory.Valid("etl"));

            await service.StartAsync(CancellationToken.None);
            try
            {
                await _logs.WaitForAsync(e => e.Message.Contains("Próxima ejecución"), Wait);

                _logs.Entries.Should().Contain(e => e.Level == LogLevel.Information && e.Message == "Orquestador iniciado con 1 trabajo(s) programado(s).");
                _logs.Entries.Should().Contain(e =>
                    e.Message.StartsWith("[etl] Programado con expresión cron '0 0 1 * *' (zona horaria: UTC)")
                    && e.Category == "CronJobRunnerLoop.etl");
                service.ExecuteTask!.IsCompleted.Should().BeFalse("el servicio vive hasta que se apague el host");
                _runner.Calls.Should().BeEmpty();
            }
            finally
            {
                await service.StopAsync(CancellationToken.None);
            }

            service.ExecuteTask!.IsCompletedSuccessfully.Should().BeTrue();
        }

        [Fact]
        public async Task StartAsync_VariosTrabajos_ArrancaUnBucleIndependientePorCadaUnoHabilitado()
        {
            var disabled = JobFactory.Valid("apagado");
            disabled.Enabled = false;
            var service = CreateService(JobFactory.Valid("a"), JobFactory.Valid("b", cron: "0 0 15 * *"), disabled);

            await service.StartAsync(CancellationToken.None);
            try
            {
                await _logs.WaitForAsync(e => e.Message.StartsWith("[a] Próxima ejecución"), Wait);
                await _logs.WaitForAsync(e => e.Message.StartsWith("[b] Próxima ejecución"), Wait);

                _logs.Entries.Should().Contain(e => e.Message == "Orquestador iniciado con 2 trabajo(s) programado(s).");
                _logs.Entries.Should().NotContain(e => e.Message.Contains("[apagado]"));
            }
            finally
            {
                await service.StopAsync(CancellationToken.None);
            }

            service.ExecuteTask!.IsCompletedSuccessfully.Should().BeTrue();
        }

        [Fact]
        public async Task StartAsync_SinZonaHoraria_UsaLaZonaHorariaLocalDelServidor()
        {
            var job = JobFactory.Valid("local");
            job.TimeZoneId = null;
            var service = CreateService(job);

            await service.StartAsync(CancellationToken.None);
            try
            {
                var entry = await _logs.WaitForAsync(e => e.Message.StartsWith("[local] Programado"), Wait);

                entry.Message.Should().Contain($"(zona horaria: {TimeZoneInfo.Local.Id})");
            }
            finally
            {
                await service.StopAsync(CancellationToken.None);
            }
        }

        [Fact]
        public async Task StartAsync_ZonaHorariaEnBlanco_TambienUsaLaZonaHorariaLocal()
        {
            var job = JobFactory.Valid("blanco");
            job.TimeZoneId = "   ";
            var service = CreateService(job);

            await service.StartAsync(CancellationToken.None);
            try
            {
                var entry = await _logs.WaitForAsync(e => e.Message.StartsWith("[blanco] Programado"), Wait);

                entry.Message.Should().Contain($"(zona horaria: {TimeZoneInfo.Local.Id})");
            }
            finally
            {
                await service.StopAsync(CancellationToken.None);
            }
        }

        [Fact]
        public async Task StartAsync_CancelacionDelHostAntesDeArrancar_TerminaSinEjecutarNingunTrabajo()
        {
            var service = CreateService(JobFactory.Valid("etl"));
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await service.StartAsync(cts.Token);
            await service.StopAsync(CancellationToken.None);

            service.ExecuteTask!.IsFaulted.Should().BeFalse();
            _runner.Calls.Should().BeEmpty();
            _logs.Entries.Should().NotContain(e => e.Message.Contains("Iniciando ejecución"));
        }
    }
}
