using System.Diagnostics;
using Connection360.Etl.Orchestrator.Configuration;
using Microsoft.Extensions.Logging;

namespace Connection360.Etl.Orchestrator.Execution
{
    /// <inheritdoc cref="IExternalProcessRunner"/>
    public sealed class ExternalProcessRunner : IExternalProcessRunner
    {
        private readonly ILogger<ExternalProcessRunner> _logger;

        public ExternalProcessRunner(ILogger<ExternalProcessRunner> logger)
        {
            _logger = logger;
        }

        public async Task<ProcessRunResult> RunAsync(CronJobSettings job, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            String workingDirectory = ResolveWorkingDirectory(job.WorkingDirectory);

            // Se registra siempre la ruta ABSOLUTA ya resuelta (no la relativa del appsettings):
            // así, si "WorkingDirectory" quedó mal calculado -por ejemplo apuntando a una carpeta
            // que no existe, como pasó al confundir cuántos "../" hacen falta para salir de
            // bin/Debug/net10.0- se ve inmediatamente en la pantalla del orquestador, en vez de
            // enterarse solo por un error críptico del sistema operativo al lanzar el proceso.
            _logger.LogInformation("[{Job}] Working directory resuelto: {Dir}", job.Name, workingDirectory);

            if (!Directory.Exists(workingDirectory))
            {
                return Fail(
                    job,
                    $"La carpeta de trabajo '{workingDirectory}' no existe. Revisa 'WorkingDirectory' del trabajo '{job.Name}' en el appsettings del orquestador " +
                    "(debe apuntar a la carpeta donde está compilado/publicado Connection360.Etl.App, sea con una ruta absoluta o relativa al directorio base del orquestador).",
                    stopwatch.Elapsed);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = job.ExecutablePath,
                Arguments = job.Arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = new Process { StartInfo = startInfo };

            // Cada línea que la ETL escribe a stdout/stderr se retransmite tal cual al logger del
            // orquestador -que a su vez la imprime en su propia consola-, con el nombre del trabajo
            // como prefijo: esto es lo que hace que "la pantalla" del orquestador muestre en vivo
            // los mismos logs que la ETL hubiera mostrado si se ejecutara directamente.
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                    _logger.LogInformation("[{Job}] {Line}", job.Name, e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                    _logger.LogWarning("[{Job}] {Line}", job.Name, e.Data);
            };

            using CancellationTokenSource? timeoutCts = job.TimeoutMinutes is Int32 minutes
                ? new CancellationTokenSource(TimeSpan.FromMinutes(minutes))
                : null;
            using CancellationTokenSource linkedCts = timeoutCts is null
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                if (!process.Start())
                    return Fail(job, "El proceso no pudo iniciarse (Process.Start devolvió false).", stopwatch.Elapsed);

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync(linkedCts.Token);

                Boolean success = process.ExitCode == 0;
                if (!success)
                    _logger.LogError("[{Job}] Terminó con código de salida {ExitCode}.", job.Name, process.ExitCode);

                return new ProcessRunResult(success, process.ExitCode, stopwatch.Elapsed, success ? null : $"Código de salida {process.ExitCode}.");
            }
            catch (OperationCanceledException) when (timeoutCts is not null && timeoutCts.IsCancellationRequested)
            {
                KillProcessTree(process);
                return Fail(job, $"Se agotó el tiempo máximo configurado ({job.TimeoutMinutes} minuto(s)); el proceso fue terminado.", stopwatch.Elapsed);
            }
            catch (OperationCanceledException)
            {
                // Cancelación del propio orquestador (Ctrl+C / apagado del host): se mata el
                // proceso hijo para no dejarlo huérfano, y se propaga la cancelación tal cual (no
                // es un fallo del trabajo, es un apagado ordenado).
                KillProcessTree(process);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Job}] Error inesperado al ejecutar el proceso.", job.Name);
                return Fail(job, ex.Message, stopwatch.Elapsed);
            }
        }

        private ProcessRunResult Fail(CronJobSettings job, String message, TimeSpan duration)
        {
            _logger.LogError("[{Job}] {Message}", job.Name, message);
            return new ProcessRunResult(false, null, duration, message);
        }

        private static void KillProcessTree(Process process)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Best effort: si el proceso ya terminó justo en este instante, o el sistema
                // operativo no permite matarlo, no hay nada más que se pueda hacer acá.
            }
        }

        private static String ResolveWorkingDirectory(String configuredPath)
        {
            return Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));
        }
    }
}
