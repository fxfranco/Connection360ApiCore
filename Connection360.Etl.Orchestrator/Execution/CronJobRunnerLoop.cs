using Connection360.Etl.Orchestrator.Configuration;
using Connection360.Etl.Orchestrator.Scheduling;
using Microsoft.Extensions.Logging;

namespace Connection360.Etl.Orchestrator.Execution
{
    /// <summary>
    /// Ciclo de vida completo de UN trabajo programado: calcula su próxima ejecución según la
    /// expresión cron, espera hasta ese momento, lo ejecuta, registra el resultado y vuelve a
    /// calcular la siguiente -indefinidamente, hasta que se cancele el token (apagado del host).
    /// Cada trabajo corre en su propio bucle independiente (ver <see cref="Hosting.OrchestratorHostedService"/>),
    /// así que una corrida larga de un trabajo nunca retrasa la programación de otro.
    /// </summary>
    public sealed class CronJobRunnerLoop
    {
        private readonly CronJobSettings _settings;
        private readonly CronJobSchedule _schedule;
        private readonly IExternalProcessRunner _processRunner;
        private readonly ILogger _logger;

        public CronJobRunnerLoop(CronJobSettings settings, CronJobSchedule schedule, IExternalProcessRunner processRunner, ILogger logger)
        {
            _settings = settings;
            _schedule = schedule;
            _processRunner = processRunner;
            _logger = logger;
        }

        /// <summary>
        /// Máxima espera que se le pasa de una sola vez a <see cref="Task.Delay(TimeSpan, CancellationToken)"/>:
        /// ese método no admite más de ~49.7 días (<c>UInt32.MaxValue - 1</c> ms) y lanzaría
        /// <see cref="ArgumentOutOfRangeException"/>, lo que pasaría con expresiones como "0 0 1 1 *"
        /// (una vez al año). Las esperas más largas se parten en tramos de este tamaño.
        /// </summary>
        public static readonly TimeSpan MaxDelayChunk = TimeSpan.FromDays(30);

        public static async Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            while (delay > TimeSpan.Zero)
            {
                TimeSpan chunk = delay > MaxDelayChunk ? MaxDelayChunk : delay;
                await Task.Delay(chunk, cancellationToken);
                delay -= chunk;
            }
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[{Job}] Programado con expresión cron '{Cron}' (zona horaria: {TimeZone}).",
                _settings.Name, _settings.CronExpression, _schedule.TimeZone.Id);

            while (!cancellationToken.IsCancellationRequested)
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset nextRun = _schedule.GetNextOccurrenceUtc(now);
                TimeSpan delay = nextRun - now;

                _logger.LogInformation(
                    "[{Job}] Próxima ejecución: {NextRun:yyyy-MM-dd HH:mm:ss zzz} (en {Delay}).",
                    _settings.Name, TimeZoneInfo.ConvertTime(nextRun, _schedule.TimeZone), delay);

                try
                {
                    await DelayAsync(delay, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break; // Apagado del host mientras esperaba: no hay corrida que registrar.
                }

                _logger.LogInformation("[{Job}] Iniciando ejecución programada...", _settings.Name);

                ProcessRunResult result;
                try
                {
                    result = await _processRunner.RunAsync(_settings, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break; // Cancelado mientras corría: apagado del host, no un fallo del trabajo.
                }

                if (result.Success)
                {
                    _logger.LogInformation("[{Job}] Ejecución finalizada correctamente en {Duration}.", _settings.Name, result.Duration);
                }
                else
                {
                    _logger.LogError("[{Job}] Ejecución finalizada con errores en {Duration}: {Error}", _settings.Name, result.Duration, result.ErrorMessage);
                }
            }
        }
    }
}
