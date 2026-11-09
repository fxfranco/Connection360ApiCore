using Connection360.Etl.Orchestrator.Execution;
using Connection360.Etl.Orchestrator.Test.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Connection360.Etl.Orchestrator.Test.Execution
{
    public class ExternalProcessRunnerTests
    {
        private readonly ListLoggerProvider _logs = new();

        private ExternalProcessRunner CreateRunner() => new(_logs.CreateLogger<ExternalProcessRunner>());

        [Fact]
        public async Task RunAsync_ProcesoTerminaConCodigoCero_DevuelveExitoYRetransmiteSuSalida()
        {
            var job = ChildProcesses.Dotnet("--list-runtimes");

            var result = await CreateRunner().RunAsync(job, CancellationToken.None);

            result.Success.Should().BeTrue();
            result.ExitCode.Should().Be(0);
            result.ErrorMessage.Should().BeNull();
            result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
            _logs.Entries.Should().Contain(e =>
                e.Level == LogLevel.Information && e.Message.StartsWith("[dotnet-job] ") && e.Message.Contains("Microsoft.NETCore.App"));
        }

        [Fact]
        public async Task RunAsync_Siempre_RegistraLaRutaAbsolutaDeTrabajoResuelta()
        {
            var job = ChildProcesses.Dotnet("--list-runtimes");

            await CreateRunner().RunAsync(job, CancellationToken.None);

            _logs.Entries.Should().Contain(e =>
                e.Level == LogLevel.Information && e.Message == $"[dotnet-job] Working directory resuelto: {job.WorkingDirectory}");
        }

        [Fact]
        public async Task RunAsync_RutaDeTrabajoRelativa_SeResuelveContraElDirectorioBaseDelOrquestador()
        {
            var job = ChildProcesses.Dotnet("--list-runtimes");
            job.WorkingDirectory = ".";
            String expected = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "."));

            var result = await CreateRunner().RunAsync(job, CancellationToken.None);

            result.Success.Should().BeTrue();
            _logs.Entries.Should().Contain(e => e.Message == $"[dotnet-job] Working directory resuelto: {expected}");
        }

        [Fact]
        public async Task RunAsync_RutaDeTrabajoAbsoluta_SeUsaTalCual()
        {
            var job = ChildProcesses.Dotnet("--list-runtimes");
            job.WorkingDirectory = Path.GetFullPath(AppContext.BaseDirectory);

            var result = await CreateRunner().RunAsync(job, CancellationToken.None);

            result.Success.Should().BeTrue();
            _logs.Entries.Should().Contain(e => e.Message == $"[dotnet-job] Working directory resuelto: {job.WorkingDirectory}");
        }

        [Fact]
        public async Task RunAsync_ProcesoTerminaConCodigoDistintoDeCero_DevuelveFalloConElCodigo()
        {
            // Un .dll inexistente hace que el host de dotnet termine con un código de salida distinto de cero.
            var job = ChildProcesses.Dotnet("archivo-inexistente-connection360.dll");

            var result = await CreateRunner().RunAsync(job, CancellationToken.None);

            result.Success.Should().BeFalse();
            result.ExitCode.Should().NotBeNull().And.NotBe(0);
            result.ErrorMessage.Should().Be($"Código de salida {result.ExitCode}.");
            _logs.Entries.Should().Contain(e => e.Level == LogLevel.Error && e.Message.Contains("Terminó con código de salida"));
        }

        [Fact]
        public async Task RunAsync_ProcesoEscribeEnStderr_RetransmiteLasLineasComoAdvertencia()
        {
            // El mismo .dll inexistente hace que el host escriba su diagnóstico en el error estándar.
            var job = ChildProcesses.Dotnet("archivo-inexistente-connection360.dll");

            await CreateRunner().RunAsync(job, CancellationToken.None);

            _logs.Entries.Should().Contain(e => e.Level == LogLevel.Warning && e.Message.StartsWith("[dotnet-job] "));
        }

        [Fact]
        public async Task RunAsync_CarpetaDeTrabajoInexistente_DevuelveFalloSinLanzarProceso()
        {
            var job = ChildProcesses.Dotnet("--list-runtimes");
            job.WorkingDirectory = Path.Combine(Path.GetTempPath(), "connection360-no-existe-" + Guid.NewGuid().ToString("N"));

            var result = await CreateRunner().RunAsync(job, CancellationToken.None);

            result.Success.Should().BeFalse();
            result.ExitCode.Should().BeNull();
            result.ErrorMessage.Should().Contain("no existe").And.Contain(job.WorkingDirectory).And.Contain("'dotnet-job'");
            _logs.Entries.Should().Contain(e => e.Level == LogLevel.Error && e.Message.Contains("no existe"));
        }

        [Fact]
        public async Task RunAsync_EjecutableInexistente_CapturaLaExcepcionYDevuelveFallo()
        {
            var job = ChildProcesses.Dotnet("--list-runtimes");
            job.ExecutablePath = "ejecutable-que-no-existe-connection360-" + Guid.NewGuid().ToString("N");

            var result = await CreateRunner().RunAsync(job, CancellationToken.None);

            result.Success.Should().BeFalse();
            result.ExitCode.Should().BeNull();
            result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
            _logs.Entries.Should().Contain(e => e.Level == LogLevel.Error && e.Exception != null && e.Message.Contains("Error inesperado"));
        }

        [Fact]
        public async Task RunAsync_ConTimeoutDeUnMinutoYProcesoRapido_TerminaConExito()
        {
            var job = ChildProcesses.Dotnet("--list-runtimes", timeoutMinutes: 5);

            var result = await CreateRunner().RunAsync(job, CancellationToken.None);

            result.Success.Should().BeTrue();
            result.ExitCode.Should().Be(0);
        }

        [Fact]
        public async Task RunAsync_TimeoutAgotado_MataElProcesoYDevuelveFallo()
        {
            // 0 minutos: el timeout ya está vencido al arrancar, así que el proceso (que dura ~30 s) se mata de inmediato.
            var job = ChildProcesses.LongRunning(timeoutMinutes: 0);

            var result = await CreateRunner().RunAsync(job, CancellationToken.None);

            result.Success.Should().BeFalse();
            result.ExitCode.Should().BeNull();
            result.ErrorMessage.Should().Contain("tiempo máximo").And.Contain("0 minuto(s)");
            result.Duration.Should().BeLessThan(TimeSpan.FromSeconds(20));
            _logs.Entries.Should().Contain(e => e.Level == LogLevel.Error && e.Message.Contains("tiempo máximo"));
        }

        [Fact]
        public async Task RunAsync_TokenYaCancelado_MataElProcesoYPropagaLaCancelacion()
        {
            var job = ChildProcesses.LongRunning();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Func<Task> act = () => CreateRunner().RunAsync(job, cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task RunAsync_CancelacionDuranteLaEjecucion_MataElProcesoYPropagaLaCancelacion()
        {
            var job = ChildProcesses.LongRunning();
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(300));
            var started = DateTime.UtcNow;

            Func<Task> act = () => CreateRunner().RunAsync(job, cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
            (DateTime.UtcNow - started).Should().BeLessThan(TimeSpan.FromSeconds(20));
        }

        [Fact]
        public async Task RunAsync_CancelacionConTimeoutConfiguradoPeroNoVencido_PropagaLaCancelacionEnVezDeReportarTimeout()
        {
            var job = ChildProcesses.LongRunning(timeoutMinutes: 10);
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(300));

            Func<Task> act = () => CreateRunner().RunAsync(job, cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
    }
}
