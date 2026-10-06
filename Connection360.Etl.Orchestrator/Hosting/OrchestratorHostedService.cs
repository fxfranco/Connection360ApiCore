using Connection360.Etl.Orchestrator.Configuration;
using Connection360.Etl.Orchestrator.Execution;
using Connection360.Etl.Orchestrator.Scheduling;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Connection360.Etl.Orchestrator.Hosting
{
    /// <summary>
    /// Hosted service raíz del orquestador: valida la configuración, y por cada trabajo habilitado
    /// arranca un <see cref="CronJobRunnerLoop"/> independiente. Solo termina cuando se cancela el
    /// token de apagado del host (Ctrl+C, o el sistema operativo deteniendo el proceso).
    /// </summary>
    public sealed class OrchestratorHostedService : BackgroundService
    {
        private readonly OrchestratorSettings _settings;
        private readonly IExternalProcessRunner _processRunner;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<OrchestratorHostedService> _logger;

        public OrchestratorHostedService(
            IOptions<OrchestratorSettings> settings,
            IExternalProcessRunner processRunner,
            ILoggerFactory loggerFactory,
            ILogger<OrchestratorHostedService> logger)
        {
            _settings = settings.Value;
            _processRunner = processRunner;
            _loggerFactory = loggerFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var enabledJobs = _settings.CronJobs.Where(job => job.Enabled).ToList();

            if (enabledJobs.Count == 0)
            {
                _logger.LogWarning(
                    "No hay trabajos habilitados en la sección '{Section}' del appsettings; el orquestador no tiene nada que programar.",
                    OrchestratorSettings.SectionName);
                return;
            }

            foreach (var job in enabledJobs)
            {
                Validate(job);
            }

            var loops = enabledJobs
                .Select(job =>
                {
                    var schedule = BuildSchedule(job);
                    var jobLogger = _loggerFactory.CreateLogger($"{nameof(CronJobRunnerLoop)}.{job.Name}");
                    var loop = new CronJobRunnerLoop(job, schedule, _processRunner, jobLogger);
                    return loop.RunAsync(stoppingToken);
                })
                .ToArray();

            _logger.LogInformation("Orquestador iniciado con {Count} trabajo(s) programado(s).", loops.Length);

            await Task.WhenAll(loops);
        }

        private static CronJobSchedule BuildSchedule(CronJobSettings job)
        {
            var expression = CronExpression.Parse(job.CronExpression);

            TimeZoneInfo timeZone = String.IsNullOrWhiteSpace(job.TimeZoneId)
                ? TimeZoneInfo.Local
                : TimeZoneInfo.FindSystemTimeZoneById(job.TimeZoneId);

            var schedule = new CronJobSchedule(expression, timeZone);

            // Falla rápido (al arrancar, con el nombre del trabajo) si la expresión es sintácticamente
            // válida pero imposible de cumplir (por ejemplo "0 0 31 2 *"), en vez de caerse más tarde
            // dentro del bucle de ejecución al calcular la próxima ocurrencia.
            try
            {
                schedule.GetNextOccurrenceUtc(DateTimeOffset.UtcNow);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException($"El trabajo '{job.Name}' tiene una expresión cron que nunca se cumple: {ex.Message}", ex);
            }

            return schedule;
        }

        /// <exception cref="InvalidOperationException">Al trabajo le falta un campo obligatorio en el appsettings.</exception>
        private static void Validate(CronJobSettings job)
        {
            if (String.IsNullOrWhiteSpace(job.Name))
                throw new InvalidOperationException("Un trabajo en 'Orchestrator:CronJobs' no tiene 'Name' configurado.");
            if (String.IsNullOrWhiteSpace(job.CronExpression))
                throw new InvalidOperationException($"El trabajo '{job.Name}' no tiene 'CronExpression' configurada.");
            if (String.IsNullOrWhiteSpace(job.ExecutablePath))
                throw new InvalidOperationException($"El trabajo '{job.Name}' no tiene 'ExecutablePath' configurado.");
            if (String.IsNullOrWhiteSpace(job.Arguments))
                throw new InvalidOperationException($"El trabajo '{job.Name}' no tiene 'Arguments' configurado.");
            if (String.IsNullOrWhiteSpace(job.WorkingDirectory))
                throw new InvalidOperationException($"El trabajo '{job.Name}' no tiene 'WorkingDirectory' configurado.");
        }
    }
}
