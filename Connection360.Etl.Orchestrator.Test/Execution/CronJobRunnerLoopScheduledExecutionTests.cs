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
    /// Fixture que arranca, una sola vez, varios bucles reales con la expresión "* * * * *" (cada
    /// minuto) y espera a que todos lleguen a su primera ejecución. El bucle usa el reloj real, así
    /// que la espera es la que falta hasta el próximo minuto calendario: como máximo ~60 s, y se
    /// paga una sola vez para todas las pruebas de la clase (los bucles corren en paralelo).
    /// </summary>
    public sealed class ScheduledLoopsFixture : IAsyncLifetime
    {
        private static readonly TimeSpan MaxWait = TimeSpan.FromSeconds(100);

        private readonly CancellationTokenSource _shutdown = new();
        private readonly CancellationTokenSource _runningCancel = new();

        public sealed class Scenario
        {
            public ListLoggerProvider Logs { get; } = new();
            public FakeProcessRunner Runner { get; init; } = default!;
            public Task Loop { get; set; } = Task.CompletedTask;
        }

        public Scenario Success { get; private set; } = default!;
        public Scenario Failure { get; private set; } = default!;
        public Scenario CancelledByShutdownWhileRunning { get; private set; } = default!;
        public Scenario RunnerThrowsCancellation { get; private set; } = default!;
        public Scenario RunnerThrowsUnexpected { get; private set; } = default!;

        public async Task InitializeAsync()
        {
            Success = new Scenario { Runner = FakeProcessRunner.Succeeding() };
            Failure = new Scenario { Runner = FakeProcessRunner.Failing("boom") };
            CancelledByShutdownWhileRunning = new Scenario
            {
                // Se queda "ejecutando" hasta que se cancela el token del host.
                Runner = new FakeProcessRunner(async (_, ct) =>
                {
                    await Task.Delay(Timeout.Infinite, ct);
                    return new ProcessRunResult(true, 0, TimeSpan.Zero, null);
                })
            };
            RunnerThrowsCancellation = new Scenario
            {
                Runner = new FakeProcessRunner((_, _) => throw new OperationCanceledException("cancelado por el runner"))
            };
            RunnerThrowsUnexpected = new Scenario
            {
                Runner = new FakeProcessRunner((_, _) => throw new InvalidOperationException("fallo inesperado del runner"))
            };

            Start(Success, _shutdown.Token);
            Start(Failure, _shutdown.Token);
            Start(CancelledByShutdownWhileRunning, _runningCancel.Token);
            Start(RunnerThrowsCancellation, _shutdown.Token);
            Start(RunnerThrowsUnexpected, _shutdown.Token);

            // Espera a que cada runner haya sido invocado por primera vez (próximo minuto calendario).
            await Task.WhenAll(
                Success.Runner.FirstCall,
                Failure.Runner.FirstCall,
                CancelledByShutdownWhileRunning.Runner.FirstCall,
                RunnerThrowsCancellation.Runner.FirstCall,
                RunnerThrowsUnexpected.Runner.FirstCall).WaitAsync(MaxWait);

            // Apagado "durante la corrida" del escenario que quedó ejecutando.
            _runningCancel.Cancel();

            // Los resultados de éxito/fallo se registran justo después de volver el runner.
            await Success.Logs.WaitForAsync(e => e.Message.Contains("finalizada correctamente"), TimeSpan.FromSeconds(10));
            await Failure.Logs.WaitForAsync(e => e.Message.Contains("finalizada con errores"), TimeSpan.FromSeconds(10));
        }

        private void Start(Scenario scenario, CancellationToken token)
        {
            var job = JobFactory.Valid("job", cron: "* * * * *");
            var schedule = new CronJobSchedule(CronExpression.Parse(job.CronExpression), TimeZoneInfo.Utc);
            var loop = new CronJobRunnerLoop(job, schedule, scenario.Runner, scenario.Logs.CreateLogger("loop"));
            scenario.Loop = Task.Run(() => loop.RunAsync(token));
        }

        public async Task DisposeAsync()
        {
            _shutdown.Cancel();
            _runningCancel.Cancel();

            foreach (var scenario in new[] { Success, Failure, CancelledByShutdownWhileRunning, RunnerThrowsCancellation, RunnerThrowsUnexpected })
            {
                try
                {
                    await scenario.Loop.WaitAsync(TimeSpan.FromSeconds(10));
                }
                catch (InvalidOperationException)
                {
                    // Esperado en RunnerThrowsUnexpected: el bucle propaga la excepción del runner.
                }
            }

            _shutdown.Dispose();
            _runningCancel.Dispose();
        }
    }

    public class CronJobRunnerLoopScheduledExecutionTests : IClassFixture<ScheduledLoopsFixture>
    {
        private readonly ScheduledLoopsFixture _fixture;

        public CronJobRunnerLoopScheduledExecutionTests(ScheduledLoopsFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void RunAsync_LlegaLaHora_EjecutaElTrabajoConSuConfiguracion()
        {
            var calls = _fixture.Success.Runner.Calls;

            calls.Should().HaveCountGreaterThanOrEqualTo(1);
            calls[0].Name.Should().Be("job");
            calls[0].CronExpression.Should().Be("* * * * *");
            _fixture.Success.Logs.Entries.Should().Contain(e => e.Message == "[job] Iniciando ejecución programada...");
        }

        [Fact]
        public void RunAsync_EjecucionExitosa_RegistraLaDuracionComoInformacion()
        {
            _fixture.Success.Logs.Entries.Should().Contain(e =>
                e.Level == LogLevel.Information && e.Message.StartsWith("[job] Ejecución finalizada correctamente en 00:00:01"));
        }

        [Fact]
        public void RunAsync_EjecucionConErrores_RegistraErrorConElDetalle()
        {
            _fixture.Failure.Logs.Entries.Should().Contain(e =>
                e.Level == LogLevel.Error && e.Message.StartsWith("[job] Ejecución finalizada con errores en 00:00:02") && e.Message.EndsWith(": boom"));
            _fixture.Failure.Logs.Entries.Should().NotContain(e => e.Message.Contains("finalizada correctamente"));
        }

        [Fact]
        public void RunAsync_TrasUnaEjecucion_VuelveAProgramarLaSiguiente()
        {
            // Anuncio inicial + anuncio tras la primera corrida.
            _fixture.Success.Logs.Entries.Count(e => e.Message.StartsWith("[job] Próxima ejecución")).Should().BeGreaterThanOrEqualTo(2);
        }

        [Fact]
        public async Task RunAsync_ApagadoDuranteLaCorrida_TerminaSinRegistrarResultado()
        {
            await _fixture.CancelledByShutdownWhileRunning.Loop.WaitAsync(TimeSpan.FromSeconds(10));

            _fixture.CancelledByShutdownWhileRunning.Loop.IsCompletedSuccessfully.Should().BeTrue();
            _fixture.CancelledByShutdownWhileRunning.Logs.Entries.Should().NotContain(e =>
                e.Message.Contains("finalizada correctamente") || e.Message.Contains("finalizada con errores"));
        }

        [Fact]
        public async Task RunAsync_RunnerLanzaOperationCanceled_SeTrataComoApagadoYTermina()
        {
            await _fixture.RunnerThrowsCancellation.Loop.WaitAsync(TimeSpan.FromSeconds(10));

            _fixture.RunnerThrowsCancellation.Loop.IsCompletedSuccessfully.Should().BeTrue();
            _fixture.RunnerThrowsCancellation.Runner.Calls.Should().HaveCount(1);
            _fixture.RunnerThrowsCancellation.Logs.Entries.Should().NotContain(e => e.Level == LogLevel.Error);
        }

        [Fact]
        public async Task RunAsync_RunnerLanzaExcepcionInesperada_LaPropagaYElBucleTerminaFallido()
        {
            Func<Task> act = () => _fixture.RunnerThrowsUnexpected.Loop.WaitAsync(TimeSpan.FromSeconds(10));

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("fallo inesperado del runner");
        }
    }
}
