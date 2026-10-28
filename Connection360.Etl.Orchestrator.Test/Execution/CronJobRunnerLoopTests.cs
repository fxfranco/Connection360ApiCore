using Connection360.Etl.Orchestrator.Configuration;
using Connection360.Etl.Orchestrator.Execution;
using Connection360.Etl.Orchestrator.Scheduling;
using Connection360.Etl.Orchestrator.Test.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Connection360.Etl.Orchestrator.Test.Execution
{
    /// <summary>
    /// Pruebas del bucle de un trabajo que NO dependen de que llegue la hora de ejecución: arranque,
    /// anuncio de la próxima ejecución y apagado. El camino posterior a la espera (ejecutar, registrar
    /// resultado, cancelar durante la corrida) se cubre en <see cref="CronJobRunnerLoopScheduledExecutionTests"/>.
    /// </summary>
    public class CronJobRunnerLoopTests
    {
        private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

        private readonly ListLoggerProvider _logs = new();
        private readonly FakeProcessRunner _runner = FakeProcessRunner.Succeeding();

        private CronJobRunnerLoop CreateLoop(CronJobSettings job, TimeZoneInfo? timeZone = null)
        {
            var schedule = new CronJobSchedule(CronExpression.Parse(job.CronExpression), timeZone ?? TimeZoneInfo.Utc);
            return new CronJobRunnerLoop(job, schedule, _runner, _logs.CreateLogger("loop"));
        }

        [Fact]
        public async Task RunAsync_TokenYaCancelado_SoloRegistraElProgramadoYTermina()
        {
            var loop = CreateLoop(JobFactory.Valid("etl"));
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await loop.RunAsync(cts.Token);

            _logs.Entries.Should().ContainSingle();
            _logs.Entries[0].Level.Should().Be(LogLevel.Information);
            _logs.Entries[0].Message.Should().Be("[etl] Programado con expresión cron '0 0 1 * *' (zona horaria: UTC).");
            _runner.Calls.Should().BeEmpty();
        }

        [Fact]
        public async Task RunAsync_ZonaHorariaPersonalizada_LaRegistraEnElMensajeInicial()
        {
            var zone = TimeZoneInfo.CreateCustomTimeZone("TEST_UTC_MINUS_5", TimeSpan.FromHours(-5), "UTC-5", "UTC-5");
            var loop = CreateLoop(JobFactory.Valid("etl"), zone);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await loop.RunAsync(cts.Token);

            _logs.Entries[0].Message.Should().Contain("(zona horaria: TEST_UTC_MINUS_5)");
        }

        [Fact]
        public async Task RunAsync_CancelacionMientrasEspera_TerminaSinEjecutarElTrabajo()
        {
            var loop = CreateLoop(JobFactory.Valid("etl"));
            using var cts = new CancellationTokenSource();

            var running = loop.RunAsync(cts.Token);
            var next = await _logs.WaitForAsync(e => e.Message.StartsWith("[etl] Próxima ejecución"), Wait);
            running.IsCompleted.Should().BeFalse("el bucle debe quedar esperando hasta la próxima ejecución");

            cts.Cancel();
            await running.WaitAsync(Wait);

            next.Level.Should().Be(LogLevel.Information);
            next.Message.Should().MatchRegex(@"^\[etl\] Próxima ejecución: \d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2} [+-]\d{2}:\d{2} \(en .+\)\.$");
            _runner.Calls.Should().BeEmpty();
            _logs.Entries.Should().NotContain(e => e.Message.Contains("Iniciando ejecución"));
        }

        [Fact]
        public async Task RunAsync_ProximaEjecucion_SeMuestraEnLaZonaHorariaDelTrabajo()
        {
            var zone = TimeZoneInfo.CreateCustomTimeZone("TEST_UTC_MINUS_5", TimeSpan.FromHours(-5), "UTC-5", "UTC-5");
            var loop = CreateLoop(JobFactory.Valid("etl"), zone);
            using var cts = new CancellationTokenSource();

            var running = loop.RunAsync(cts.Token);
            var next = await _logs.WaitForAsync(e => e.Message.StartsWith("[etl] Próxima ejecución"), Wait);
            cts.Cancel();
            await running.WaitAsync(Wait);

            next.Message.Should().Contain("-05:00");
        }
    }
}
