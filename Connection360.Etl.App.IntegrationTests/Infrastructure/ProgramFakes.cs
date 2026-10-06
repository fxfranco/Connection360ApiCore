using Connection360.Etl.Application.DTOs;
using Connection360.Etl.Application.Ports;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Connection360.Etl.App.IntegrationTests.Infrastructure
{
    /// <summary>Registra qué pasó dentro del host ejecutado por Program.cs (orden de pasos, scopes, tokens, singletons).</summary>
    public sealed class ProgramProbe
    {
        private readonly List<String> _calls = new();

        public IReadOnlyList<String> Calls
        {
            get
            {
                lock (_calls)
                    return _calls.ToArray();
            }
        }

        public List<Guid> ScopeIds { get; } = new();
        public List<Boolean> TokenWasCancellable { get; } = new();
        public List<Boolean> TokenWasCancelled { get; } = new();
        public List<NpgsqlDataSource> DataSources { get; } = new();
        public Int32 ScopesDisposed;

        public void Record(String call)
        {
            lock (_calls)
                _calls.Add(call);
        }
    }

    /// <summary>Servicio scoped que permite verificar que cada paso corre en su propio scope y que éste se libera.</summary>
    public sealed class ScopeMarker : IAsyncDisposable
    {
        private readonly ProgramProbe _probe;

        public ScopeMarker(ProgramProbe probe) => _probe = probe;

        public Guid Id { get; } = Guid.NewGuid();

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _probe.ScopesDisposed);
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>Casos de uso falsos que sustituyen a los reales en el host de Program.cs.</summary>
    public sealed class ProgramFakes
    {
        public ProgramProbe Probe { get; } = new();

        public Func<EtlRunResult> LogsResult { get; set; } = () => SuccessResult(transformed: 3, loaded: 3, ("DATALOGS", 3));
        public Func<EtlRunResult> MainResult { get; set; } = () => SuccessResult(transformed: 5, loaded: 4, ("BPMS", 5), ("SIM", 2));
        public Func<Int32> PurgeResult { get; set; } = () => 4;

        public static EtlRunResult SuccessResult(Int32 transformed, Int32 loaded, params (String Api, Int32 Count)[] extracted)
        {
            var result = new EtlRunResult
            {
                Success = true,
                TransformedRecords = transformed,
                LoadedRecords = loaded,
                StartedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                FinishedAtUtc = new DateTime(2026, 1, 1, 0, 0, 5, DateTimeKind.Utc),
            };
            foreach (var (api, count) in extracted)
                result.ExtractedRecordsByApi[api] = count;
            return result;
        }

        public static EtlRunResult FailureResult(String error) => new()
        {
            Success = false,
            ErrorMessage = error,
            StartedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            FinishedAtUtc = new DateTime(2026, 1, 1, 0, 0, 1, DateTimeKind.Utc),
        };

        public void Register(IServiceCollection services)
        {
            services.AddSingleton(Probe);
            services.AddScoped<ScopeMarker>();
            services.AddScoped<IRunLogsEtlProcessUseCase>(sp => new FakeLogs(this, sp));
            services.AddScoped<IRunEtlProcessUseCase>(sp => new FakeMain(this, sp));
            services.AddScoped<IPurgeEtlJobControlUseCase>(sp => new FakePurge(this, sp));
        }

        private void Observe(String step, IServiceProvider sp, CancellationToken token)
        {
            Probe.Record(step);
            Probe.ScopeIds.Add(sp.GetRequiredService<ScopeMarker>().Id);
            Probe.TokenWasCancellable.Add(token.CanBeCanceled);
            Probe.TokenWasCancelled.Add(token.IsCancellationRequested);
            Probe.DataSources.Add(sp.GetRequiredService<NpgsqlDataSource>());
        }

        private sealed class FakeLogs : IRunLogsEtlProcessUseCase
        {
            private readonly ProgramFakes _owner;
            private readonly IServiceProvider _sp;

            public FakeLogs(ProgramFakes owner, IServiceProvider sp)
            {
                _owner = owner;
                _sp = sp;
            }

            public Task<EtlRunResult> ExecuteAsync(CancellationToken cancellationToken = default)
            {
                _owner.Observe("logs", _sp, cancellationToken);
                return Task.FromResult(_owner.LogsResult());
            }
        }

        private sealed class FakeMain : IRunEtlProcessUseCase
        {
            private readonly ProgramFakes _owner;
            private readonly IServiceProvider _sp;

            public FakeMain(ProgramFakes owner, IServiceProvider sp)
            {
                _owner = owner;
                _sp = sp;
            }

            public Task<EtlRunResult> ExecuteAsync(CancellationToken cancellationToken = default)
            {
                _owner.Observe("main", _sp, cancellationToken);
                return Task.FromResult(_owner.MainResult());
            }
        }

        private sealed class FakePurge : IPurgeEtlJobControlUseCase
        {
            private readonly ProgramFakes _owner;
            private readonly IServiceProvider _sp;

            public FakePurge(ProgramFakes owner, IServiceProvider sp)
            {
                _owner = owner;
                _sp = sp;
            }

            public Task<Int32> ExecuteAsync(CancellationToken cancellationToken = default)
            {
                _owner.Observe("purge", _sp, cancellationToken);
                return Task.FromResult(_owner.PurgeResult());
            }
        }
    }
}
