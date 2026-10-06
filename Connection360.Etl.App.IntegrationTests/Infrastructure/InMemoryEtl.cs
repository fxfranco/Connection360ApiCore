using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Domain.Enums;
using Connection360.Etl.Domain.Ports.Persistence;

namespace Connection360.Etl.App.IntegrationTests.Infrastructure
{
    /// <summary>Fila de connection360write.etl_job_control en memoria.</summary>
    public sealed class EtlJobRow
    {
        public Int64 Id { get; init; }
        public EtlJobName JobName { get; init; }
        public Int32? PageSize { get; init; }
        public String Status { get; set; } = "PROCESSING";
        public Int32 LastProcessedPage { get; set; }
        public Int32 TotalRecordsProcessed { get; set; }
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// "Base de datos" en memoria del proceso ETL: reemplaza a PostgreSQL (DbSession/UnitOfWork/
    /// repositorios de Infrastructure) para poder ejecutar los casos de uso REALES de punta a punta
    /// sin base de datos.
    /// </summary>
    public sealed class InMemoryEtlStore
    {
        private Int64 _nextSheetId = 1;
        private Int64 _nextJobId = 100;

        public Dictionary<String, ApplicationDataSheet> Sheets { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<String, Int64> SheetIds { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<EtlJobRow> Jobs { get; } = new();
        public List<LogStatusTracking> LogRows { get; } = new();
        public List<OutboxMessage> Outbox { get; } = new();

        /// <summary>Secuencia de eventos transaccionales: "begin", "commit", "rollback".</summary>
        public List<String> Transactions { get; } = new();

        /// <summary>Si es true, UpsertBatchAsync lanza (para probar el rollback de la transacción de una ronda).</summary>
        public Boolean FailOnUpsert { get; set; }

        public IEnumerable<EtlJobRow> JobsOf(EtlJobName name) => Jobs.Where(j => j.JobName == name);

        internal Int64 NextSheetId() => _nextSheetId++;
        internal Int64 NextJobId() => _nextJobId++;
    }

    public sealed class InMemoryUnitOfWork : IUnitOfWork
    {
        private readonly InMemoryEtlStore _store;
        private readonly Dictionary<Type, Object> _repositories;

        public InMemoryUnitOfWork(InMemoryEtlStore store)
        {
            _store = store;
            _repositories = new Dictionary<Type, Object>
            {
                [typeof(IApplicationDataSheetRepository)] = new SheetRepository(store),
                [typeof(IEtlJobControlRepository)] = new JobControlRepository(store),
                [typeof(ILogStatusTrackingRepository)] = new LogRepository(store),
                [typeof(IOutboxMessageRepository)] = new OutboxRepository(store),
            };
        }

        public TRepository GetRepository<TRepository>() where TRepository : class =>
            (TRepository)_repositories[typeof(TRepository)];

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            _store.Transactions.Add("begin");
            return Task.CompletedTask;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            _store.Transactions.Add("commit");
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            _store.Transactions.Add("rollback");
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private sealed class SheetRepository : IApplicationDataSheetRepository
        {
            private readonly InMemoryEtlStore _store;

            public SheetRepository(InMemoryEtlStore store) => _store = store;

            public Task<Int32> UpsertBatchAsync(IEnumerable<ApplicationDataSheet> rows, CancellationToken cancellationToken = default)
            {
                if (_store.FailOnUpsert)
                    throw new InvalidOperationException("Fallo simulado de la base de datos en el upsert.");

                Int32 count = 0;
                foreach (var row in rows)
                {
                    _store.Sheets[row.DocumentoTransporteHbl] = row;
                    if (!_store.SheetIds.ContainsKey(row.DocumentoTransporteHbl))
                        _store.SheetIds[row.DocumentoTransporteHbl] = _store.NextSheetId();
                    count++;
                }

                return Task.FromResult(count);
            }

            public Task<IReadOnlyDictionary<String, ApplicationDataSheetChangeSnapshot>> GetChangeSnapshotsAsync(
                IEnumerable<String> documentNumbers, CancellationToken cancellationToken = default)
            {
                var result = new Dictionary<String, ApplicationDataSheetChangeSnapshot>(StringComparer.OrdinalIgnoreCase);
                foreach (var doc in documentNumbers)
                {
                    if (_store.Sheets.TryGetValue(doc, out var sheet))
                    {
                        result[doc] = new ApplicationDataSheetChangeSnapshot
                        {
                            Id = _store.SheetIds[doc],
                            DocumentoTransporteHbl = doc,
                            Estado = sheet.Estado,
                            Comentario = sheet.Comentario,
                            FechaComentario = sheet.FechaComentario,
                        };
                    }
                }

                return Task.FromResult<IReadOnlyDictionary<String, ApplicationDataSheetChangeSnapshot>>(result);
            }

            public Task<IReadOnlyDictionary<String, Int64>> GetIdsByDocumentAsync(
                IEnumerable<String> documentNumbers, CancellationToken cancellationToken = default)
            {
                var result = documentNumbers
                    .Where(_store.SheetIds.ContainsKey)
                    .ToDictionary(d => d, d => _store.SheetIds[d], StringComparer.OrdinalIgnoreCase);
                return Task.FromResult<IReadOnlyDictionary<String, Int64>>(result);
            }
        }

        private sealed class JobControlRepository : IEtlJobControlRepository
        {
            private readonly InMemoryEtlStore _store;

            public JobControlRepository(InMemoryEtlStore store) => _store = store;

            public Task<Boolean> HasCompletedRunAsync(EtlJobName jobName, CancellationToken cancellationToken = default) =>
                Task.FromResult(_store.JobsOf(jobName).Any(j => j.Status == "COMPLETED"));

            public Task<Int64> StartRunAsync(EtlJobName jobName, Int32? pageSize, CancellationToken cancellationToken = default)
            {
                var job = new EtlJobRow { Id = _store.NextJobId(), JobName = jobName, PageSize = pageSize };
                _store.Jobs.Add(job);
                return Task.FromResult(job.Id);
            }

            public Task RegisterPageProgressAsync(Int64 jobControlId, Int32 lastProcessedPage, Int32 recordsProcessedInPage, CancellationToken cancellationToken = default)
            {
                var job = _store.Jobs.Single(j => j.Id == jobControlId);
                job.LastProcessedPage = lastProcessedPage;
                job.TotalRecordsProcessed += recordsProcessedInPage;
                job.UpdatedAtUtc = DateTime.UtcNow;
                return Task.CompletedTask;
            }

            public Task CompleteRunAsync(Int64 jobControlId, CancellationToken cancellationToken = default)
            {
                var job = _store.Jobs.Single(j => j.Id == jobControlId);
                job.Status = "COMPLETED";
                job.UpdatedAtUtc = DateTime.UtcNow;
                return Task.CompletedTask;
            }

            public Task FailRunAsync(Int64 jobControlId, CancellationToken cancellationToken = default)
            {
                var job = _store.Jobs.Single(j => j.Id == jobControlId);
                job.Status = "FAILED";
                job.UpdatedAtUtc = DateTime.UtcNow;
                return Task.CompletedTask;
            }

            public Task<Int32> DeleteOlderThanAsync(EtlJobName jobName, Int32 olderThanDays, CancellationToken cancellationToken = default)
            {
                DateTime limit = DateTime.UtcNow.AddDays(-olderThanDays);
                Int32 removed = _store.Jobs.RemoveAll(j => j.JobName == jobName && j.UpdatedAtUtc < limit);
                return Task.FromResult(removed);
            }
        }

        private sealed class LogRepository : ILogStatusTrackingRepository
        {
            private readonly InMemoryEtlStore _store;

            public LogRepository(InMemoryEtlStore store) => _store = store;

            public Task<Int32> InsertBatchAsync(IEnumerable<LogStatusTracking> rows, CancellationToken cancellationToken = default)
            {
                var list = rows.ToList();
                _store.LogRows.AddRange(list);
                return Task.FromResult(list.Count);
            }
        }

        private sealed class OutboxRepository : IOutboxMessageRepository
        {
            private readonly InMemoryEtlStore _store;

            public OutboxRepository(InMemoryEtlStore store) => _store = store;

            public Task<Int32> InsertBatchAsync(IEnumerable<OutboxMessage> messages, CancellationToken cancellationToken = default)
            {
                var list = messages.ToList();
                _store.Outbox.AddRange(list);
                return Task.FromResult(list.Count);
            }
        }
    }
}
