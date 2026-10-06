using Connection360.Etl.Application.Ports;
using Connection360.Etl.Application.UseCases;
using Connection360.Etl.Domain.Constants;
using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Domain.Enums;
using Connection360.Etl.Domain.Interfaces;
using Connection360.Etl.Domain.Ports.Persistence;
using Moq;

namespace Connection360.Etl.Application.Tests.Support
{
    /// <summary>
    /// Arma todas las dependencias (mocks) de RunEtlProcessUseCase con comportamiento por defecto
    /// razonable y un registro ordenado de llamadas (<see cref="Calls"/>) para verificar secuencias.
    /// </summary>
    internal sealed class RunEtlHarness
    {
        public const Int64 MigrationControlId = 100;
        public const Int64 SheetControlId = 200;

        public List<String> Calls { get; } = new();

        public Mock<IExternalDataGateway> Gateway { get; } = new();
        public Mock<IDynamicDataSetMerger> Merger { get; } = new();
        public Mock<IShipmentsDataSheetMappingService> Mapping { get; } = new();
        public Mock<IUnitOfWork> UnitOfWork { get; } = new();
        public Mock<IApplicationDataSheetChangeDetector> Detector { get; } = new();
        public Mock<IEtlChangeMessageCatalog> Catalog { get; } = new();
        public Mock<IEtlJobControlRepository> JobControl { get; } = new();
        public Mock<ILogStatusTrackingRepository> LogRepository { get; } = new();
        public Mock<IOutboxMessageRepository> OutboxRepository { get; } = new();
        public Mock<IApplicationDataSheetRepository> SheetRepository { get; } = new();
        public ListLogger<RunEtlProcessUseCase> Logger { get; } = new();

        /// <summary>Páginas que devolverá la pasarela por cada API.</summary>
        public Dictionary<String, List<DynamicDataSet>> PagesByApi { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<(String Api, IDictionary<String, String> Filters)> FetchRequests { get; } = new();
        public List<(List<DynamicDataSet> DataSets, String JoinField, DataSetJoinType JoinType)> MergeCalls { get; } = new();
        public List<LogStatusTracking> InsertedLogRows { get; } = new();
        public List<OutboxMessage> InsertedOutboxMessages { get; } = new();
        public List<List<ApplicationDataSheet>> UpsertedBatches { get; } = new();

        public RunEtlHarness(Boolean migrationAlreadyCompleted = false)
        {
            UnitOfWork.Setup(u => u.GetRepository<IEtlJobControlRepository>()).Returns(JobControl.Object);
            UnitOfWork.Setup(u => u.GetRepository<ILogStatusTrackingRepository>()).Returns(LogRepository.Object);
            UnitOfWork.Setup(u => u.GetRepository<IOutboxMessageRepository>()).Returns(OutboxRepository.Object);
            UnitOfWork.Setup(u => u.GetRepository<IApplicationDataSheetRepository>()).Returns(SheetRepository.Object);

            UnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .Callback(() => Calls.Add("Begin")).Returns(Task.CompletedTask);
            UnitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                .Callback(() => Calls.Add("Commit")).Returns(Task.CompletedTask);
            UnitOfWork.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>()))
                .Callback(() => Calls.Add("Rollback")).Returns(Task.CompletedTask);

            JobControl.Setup(r => r.HasCompletedRunAsync(EtlJobName.ApplicationDataSheetMigration, It.IsAny<CancellationToken>()))
                .ReturnsAsync(migrationAlreadyCompleted);
            JobControl.Setup(r => r.StartRunAsync(It.IsAny<EtlJobName>(), It.IsAny<Int32?>(), It.IsAny<CancellationToken>()))
                .Callback((EtlJobName job, Int32? size, CancellationToken _) => Calls.Add($"Start:{job}:{size?.ToString() ?? "null"}"))
                .ReturnsAsync((EtlJobName job, Int32? _, CancellationToken _) => job == EtlJobName.ApplicationDataSheetMigration ? MigrationControlId : SheetControlId);
            JobControl.Setup(r => r.RegisterPageProgressAsync(It.IsAny<Int64>(), It.IsAny<Int32>(), It.IsAny<Int32>(), It.IsAny<CancellationToken>()))
                .Callback((Int64 id, Int32 page, Int32 count, CancellationToken _) => Calls.Add($"Progress:{id}:{page}:{count}"))
                .Returns(Task.CompletedTask);
            JobControl.Setup(r => r.CompleteRunAsync(It.IsAny<Int64>(), It.IsAny<CancellationToken>()))
                .Callback((Int64 id, CancellationToken _) => Calls.Add($"Complete:{id}"))
                .Returns(Task.CompletedTask);
            JobControl.Setup(r => r.FailRunAsync(It.IsAny<Int64>(), It.IsAny<CancellationToken>()))
                .Callback((Int64 id, CancellationToken _) => Calls.Add($"Fail:{id}"))
                .Returns(Task.CompletedTask);

            Gateway.Setup(g => g.FetchDataPagedAsync(It.IsAny<String>(), It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>()))
                .Returns((String api, IDictionary<String, String> filters, CancellationToken _) =>
                {
                    FetchRequests.Add((api, filters));
                    List<DynamicDataSet> pages = PagesByApi.TryGetValue(api, out var configured) ? configured : new List<DynamicDataSet>();
                    return AsyncSequence.From(pages.ToArray());
                });

            Merger.Setup(m => m.Merge(It.IsAny<IEnumerable<DynamicDataSet>>(), It.IsAny<String>(), It.IsAny<DataSetJoinType>()))
                .Returns((IEnumerable<DynamicDataSet> sets, String joinField, DataSetJoinType joinType) =>
                {
                    var list = sets.ToList();
                    MergeCalls.Add((list, joinField, joinType));
                    return new DynamicDataSet(new[] { joinField }, list.SelectMany(s => s.Rows));
                });

            Mapping.Setup(m => m.Map(It.IsAny<DynamicDataSet>()))
                .Returns((DynamicDataSet set) => set.Rows
                    .Select(r => new ApplicationDataSheet
                    {
                        DocumentoTransporteHbl = r[ExternalDataFields.DocumentNumber],
                        NitCliente = "NIT-" + r[ExternalDataFields.DocumentNumber],
                        Estado = "NUEVO",
                    })
                    .ToList());

            SheetRepository.Setup(r => r.UpsertBatchAsync(It.IsAny<IEnumerable<ApplicationDataSheet>>(), It.IsAny<CancellationToken>()))
                .Callback((IEnumerable<ApplicationDataSheet> rows, CancellationToken _) =>
                {
                    Calls.Add("Upsert");
                    UpsertedBatches.Add(rows.ToList());
                })
                .ReturnsAsync((IEnumerable<ApplicationDataSheet> rows, CancellationToken _) => rows.Count());
            SheetRepository.Setup(r => r.GetChangeSnapshotsAsync(It.IsAny<IEnumerable<String>>(), It.IsAny<CancellationToken>()))
                .Callback(() => Calls.Add("Snapshots"))
                .ReturnsAsync(new Dictionary<String, ApplicationDataSheetChangeSnapshot>());
            SheetRepository.Setup(r => r.GetIdsByDocumentAsync(It.IsAny<IEnumerable<String>>(), It.IsAny<CancellationToken>()))
                .Callback(() => Calls.Add("GetIds"))
                .ReturnsAsync(new Dictionary<String, Int64>());

            Detector.Setup(d => d.DetectChanges(It.IsAny<IReadOnlyList<ApplicationDataSheet>>(), It.IsAny<IReadOnlyDictionary<String, ApplicationDataSheetChangeSnapshot>>()))
                .Callback(() => Calls.Add("Detect"))
                .Returns(new List<ApplicationDataSheetChange>());

            Catalog.Setup(c => c.GetStateChangeMessage(It.IsAny<String>()))
                .Returns((String estado) => ($"Titulo-{estado}", $"Mensaje-{estado}"));
            Catalog.Setup(c => c.GetCommentChangeMessage()).Returns(("Titulo-Comentario", "Mensaje-Comentario"));

            LogRepository.Setup(r => r.InsertBatchAsync(It.IsAny<IEnumerable<LogStatusTracking>>(), It.IsAny<CancellationToken>()))
                .Callback((IEnumerable<LogStatusTracking> rows, CancellationToken _) =>
                {
                    Calls.Add("InsertLog");
                    InsertedLogRows.AddRange(rows);
                })
                .ReturnsAsync((IEnumerable<LogStatusTracking> rows, CancellationToken _) => rows.Count());
            OutboxRepository.Setup(r => r.InsertBatchAsync(It.IsAny<IEnumerable<OutboxMessage>>(), It.IsAny<CancellationToken>()))
                .Callback((IEnumerable<OutboxMessage> rows, CancellationToken _) =>
                {
                    Calls.Add("InsertOutbox");
                    InsertedOutboxMessages.AddRange(rows);
                })
                .ReturnsAsync((IEnumerable<OutboxMessage> rows, CancellationToken _) => rows.Count());
        }

        public RunEtlHarness WithPages(String api, params DynamicDataSet[] pages)
        {
            PagesByApi[api] = pages.ToList();
            return this;
        }

        public RunEtlProcessUseCase CreateUseCase(Int32? pageSize = null, String systemUser = "SISTEMA_TEST")
            => new(Gateway.Object, Merger.Object, Mapping.Object, UnitOfWork.Object, pageSize,
                   Detector.Object, Catalog.Object, systemUser, Logger);
    }
}
