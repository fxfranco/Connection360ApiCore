using Connection360.Etl.Orchestrator.Configuration;
using Connection360.Etl.Orchestrator.Execution;

namespace Connection360.Etl.Orchestrator.IntegrationTests.Infrastructure
{
    /// <summary>
    /// IExternalProcessRunner de prueba: registra cada invocación y delega en un comportamiento
    /// configurable, sin lanzar ningún proceso real.
    /// </summary>
    public sealed class FakeProcessRunner : IExternalProcessRunner
    {
        private readonly Func<CronJobSettings, CancellationToken, Task<ProcessRunResult>> _behavior;
        private readonly List<CronJobSettings> _calls = new();
        private readonly TaskCompletionSource _firstCall = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public FakeProcessRunner(Func<CronJobSettings, CancellationToken, Task<ProcessRunResult>> behavior)
        {
            _behavior = behavior;
        }

        public static FakeProcessRunner Succeeding() =>
            new((_, _) => Task.FromResult(new ProcessRunResult(true, 0, TimeSpan.FromSeconds(1), null)));

        public static FakeProcessRunner Failing(String error = "falló") =>
            new((_, _) => Task.FromResult(new ProcessRunResult(false, 1, TimeSpan.FromSeconds(2), error)));

        public IReadOnlyList<CronJobSettings> Calls
        {
            get
            {
                lock (_calls)
                    return _calls.ToArray();
            }
        }

        /// <summary>Se completa cuando el runner es invocado por primera vez.</summary>
        public Task FirstCall => _firstCall.Task;

        public Task<ProcessRunResult> RunAsync(CronJobSettings job, CancellationToken cancellationToken)
        {
            lock (_calls)
                _calls.Add(job);

            _firstCall.TrySetResult();
            return _behavior(job, cancellationToken);
        }
    }
}
