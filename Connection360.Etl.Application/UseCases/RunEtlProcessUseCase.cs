using Connection360.Etl.Application.DTOs;
using Connection360.Etl.Application.Ports;
using Connection360.Etl.Application.UseCases.Paging;
using Connection360.Etl.Domain.Constants;
using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Domain.Enums;
using Connection360.Etl.Domain.Interfaces;
using Connection360.Etl.Domain.Ports.Persistence;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Connection360.Etl.Application.UseCases
{
    /// <summary>
    /// Orquesta una corrida completa del proceso ETL:
    /// <list type="number">
    /// <item><description><b>Extract</b>: consulta cada API operativa configurada en "ExternalApi" (BPMS, SIM, OPENCOMEX, ASISCOMEX, SYSTEMCARRIER), vía <see cref="IExternalDataGateway"/>. La API "DATALOGS" NO se consulta aquí: tiene su propio proceso ETL independiente, ver <see cref="RunLogsEtlProcessUseCase"/>.</description></item>
    /// <item><description><b>Transform</b>: combina los datasets operativos por número de documento (HBL) con <see cref="IDynamicDataSetMerger"/> y los convierte en filas de bodega de datos con <see cref="IShipmentsDataSheetMappingService"/>.</description></item>
    /// <item><description><b>Load</b>: persiste el resultado en PostgreSQL (connection360write.application_data_sheet), vía <see cref="IUnitOfWork"/> / <see cref="IApplicationDataSheetRepository"/>. En la misma transacción de cada ronda también se detectan y notifican los cambios de ESTADO/COMENTARIO (ver más abajo), salvo durante la migración inicial.</description></item>
    /// </list>
    /// A diferencia de los casos de uso de Connection360.Api (que consultan las APIs filtrando/acotando por un cliente puntual para responder una petición HTTP), esta corrida es una carga completa sin filtros: el objetivo es poblar la bodega de datos con TODOS los documentos disponibles.
    /// <para>
    /// <b>Registro en connection360write.etl_job_control (job "application_data_sheet"):</b> a
    /// diferencia de <see cref="RunLogsEtlProcessUseCase"/>, este proceso SIEMPRE se ejecuta (no
    /// valida si ya hay una corrida COMPLETED previa) - cada corrida crea su propio registro nuevo.
    /// Aplica la misma mecánica de control: PROCESSING al iniciar (autocommit), avance
    /// (last_processed_page/total_records_processed) actualizado DENTRO de la transacción de cada
    /// página/ronda, y COMPLETED solo después de que la última página cargó sin error; si algo
    /// falla, el registro queda en FAILED. <c>page_size</c> se guarda con el valor recibido en
    /// <see cref="_pageSize"/> (el mismo tamaño de página GLOBAL que usan las 5 APIs operativas, ver
    /// ExternalApiSettings.PageSize) - null cuando la paginación está deshabilitada.
    /// </para>
    /// <para>
    /// <b>Paginación:</b> las 5 APIs operativas se extraen como secuencias paginadas
    /// (<c>IExternalDataGateway.FetchDataPagedAsync</c>) y se sincronizan en "rondas" con
    /// <see cref="MergedPageEnumerator"/>: en la ronda N se combinan (merge) las páginas N de cada
    /// API todavía activa y esa ronda se transforma y se carga en SU PROPIA transacción. Con la
    /// paginación deshabilitada, cada API entrega una sola "página" con el 100% de sus datos, por lo
    /// que este mismo código produce exactamente una ronda.
    /// </para>
    /// <para>
    /// <b>Migración/carga inicial (job "application_data_sheet_migration" en etl_job_control):</b>
    /// mientras no exista un registro COMPLETED de este job "marcador" (independiente del job
    /// "application_data_sheet" de arriba, que SIEMPRE se crea en cada corrida), la corrida se trata
    /// como la primera carga masiva de datos: se omite POR COMPLETO la detección/notificación de
    /// cambios (ninguna consulta de snapshot "antes", ningún <see cref="IApplicationDataSheetChangeDetector"/>,
    /// ninguna fila en log_status_tracking/outbox_messages) y solo se hace el upsert - comparar cada
    /// documento contra "nada" en ese escenario generaría un cambio (y su notificación) por cada
    /// documento de la bodega completa, lo cual no tiene sentido para una carga inicial. Cuando todas
    /// las rondas de esa corrida cargan sin error, el job de migración queda marcado COMPLETED para
    /// siempre: las corridas siguientes ya hacen la validación completa de cambios descrita abajo.
    /// </para>
    /// <para>
    /// <b>Detección de cambios de ESTADO/COMENTARIO (por documento de transporte, HBL), fuera de la
    /// migración inicial:</b> antes de hacer el upsert de cada ronda se consulta, EN LA MISMA
    /// TRANSACCIÓN, el snapshot "antes" (Id/Estado/Comentario/FechaComentario tal como están hoy en
    /// la tabla, acotado únicamente a los documentos de esa ronda vía
    /// <see cref="IApplicationDataSheetRepository.GetChangeSnapshotsAsync"/>) y se compara contra lo
    /// recién transformado ("después") con <see cref="IApplicationDataSheetChangeDetector"/>. Un
    /// documento que no tenía snapshot "antes" (no existía todavía en la tabla) también cuenta como
    /// cambio: su Id de application_data_sheet recién se genera en el upsert de esta misma ronda, así
    /// que se resuelve después con <see cref="IApplicationDataSheetRepository.GetIdsByDocumentAsync"/>,
    /// acotado solo a esos documentos nuevos. Si hay cambio de ESTADO se inserta una fila en
    /// connection360write.log_status_tracking y un mensaje en connection360write.outbox_messages
    /// (event_type "ChangeState"); si hay cambio de COMENTARIO y/o FECHA COMENTARIO se inserta
    /// únicamente el mensaje de outbox (event_type "Comment") - ver <see cref="EtlChangeNotifier"/>.
    /// Todo esto ocurre dentro de la MISMA transacción que el upsert principal y el avance de
    /// etl_job_control de esa ronda (<see cref="EtlTransactionHelper.RunInOwnTransactionAsync{TResult}"/>):
    /// si cualquier inserción falla, toda la ronda (carga + log + outbox + avance) se revierte junto,
    /// garantizando que las 3 tablas siempre queden consistentes entre sí.
    /// </para>
    /// </summary>
    public class RunEtlProcessUseCase : IRunEtlProcessUseCase
    {
        // APIs operativas que se combinan (merge) por documento de transporte para formar cada fila de la bodega de datos.
        private static readonly String[] OperationalApis = { "BPMS", "SIM", "OPENCOMEX", "ASISCOMEX", "SYSTEMCARRIER" };

        private readonly IExternalDataGateway _externalDataGateway;
        private readonly IDynamicDataSetMerger _merger;
        private readonly IShipmentsDataSheetMappingService _mappingService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly Int32? _pageSize;
        private readonly IApplicationDataSheetChangeDetector _changeDetector;
        private readonly IEtlChangeMessageCatalog _messageCatalog;
        private readonly String _changeTrackingSystemUser;
        private readonly ILogger<RunEtlProcessUseCase> _logger;

        /// <param name="pageSize">
        /// Tamaño de página GLOBAL configurado para las 5 APIs operativas (ExternalApiSettings.PageSize),
        /// ya resuelto por la composición de dependencias de Connection360.Etl.App - null si la
        /// paginación está deshabilitada. Se pasa como un valor plano (no como el objeto de
        /// configuración de Infrastructure) para no romper la arquitectura hexagonal: Application no
        /// puede depender de Infrastructure.
        /// </param>
        /// <param name="changeTrackingSystemUser">
        /// Usuario "de sistema" (EtlChangeTrackingSettings.SystemUser, Infrastructure) que queda
        /// registrado como autor de cada cambio de estado detectado - se pasa como String plano por
        /// la misma razón que <paramref name="pageSize"/>.
        /// </param>
        public RunEtlProcessUseCase(
            IExternalDataGateway externalDataGateway,
            IDynamicDataSetMerger merger,
            IShipmentsDataSheetMappingService mappingService,
            IUnitOfWork unitOfWork,
            Int32? pageSize,
            IApplicationDataSheetChangeDetector changeDetector,
            IEtlChangeMessageCatalog messageCatalog,
            String changeTrackingSystemUser,
            ILogger<RunEtlProcessUseCase> logger)
        {
            _externalDataGateway = externalDataGateway;
            _merger = merger;
            _mappingService = mappingService;
            _unitOfWork = unitOfWork;
            _pageSize = pageSize;
            _changeDetector = changeDetector;
            _messageCatalog = messageCatalog;
            _changeTrackingSystemUser = changeTrackingSystemUser;
            _logger = logger;
        }

        public async Task<EtlRunResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var result = new EtlRunResult { StartedAtUtc = DateTime.UtcNow };
            Int32 pageNumber = 0;
            Boolean isInitialMigration = false;

            var jobControlRepository = _unitOfWork.GetRepository<IEtlJobControlRepository>();
            var jobControl = new EtlJobControlTracker(jobControlRepository, EtlJobName.ApplicationDataSheet);

            // Job "marcador", independiente del anterior: mientras no tenga un COMPLETED, esta y
            // cualquier corrida futura se tratan como la migración inicial (ver comentario de clase).
            var migrationJobControl = new EtlJobControlTracker(jobControlRepository, EtlJobName.ApplicationDataSheetMigration);

            // Se resuelven una sola vez (sobre el mismo DbSession/UnitOfWork de toda la corrida, igual
            // que jobControlRepository arriba): cada uno participa correctamente en la transacción que
            // esté activa en cada ronda porque leen la transacción vigente al momento de cada llamada,
            // no al resolverse.
            var logStatusTrackingRepository = _unitOfWork.GetRepository<ILogStatusTrackingRepository>();
            var outboxMessageRepository = _unitOfWork.GetRepository<IOutboxMessageRepository>();
            var changeNotifier = new EtlChangeNotifier(logStatusTrackingRepository, outboxMessageRepository, _messageCatalog, _changeTrackingSystemUser);

            try
            {
                _logger.LogInformation("Iniciando corrida del proceso ETL.");

                // ¿Ya hubo una migración inicial COMPLETED? Si no, esta corrida completa (todas sus
                // rondas) se hace en modo migración: solo carga, sin detectar ni notificar cambios.
                isInitialMigration = !await migrationJobControl.HasCompletedRunAsync(cancellationToken);

                if (isInitialMigration)
                {
                    _logger.LogInformation(
                        "No hay una migración inicial COMPLETED registrada (job 'application_data_sheet_migration'): esta corrida se trata como la carga inicial de application_data_sheet, sin detección de cambios.");
                    await migrationJobControl.StartAsync(_pageSize, cancellationToken);
                }

                // Este job siempre se ejecuta: no se valida si ya hay un COMPLETED previo. Cada
                // corrida registra su propio renglón de control (PROCESSING, autocommit).
                await jobControl.StartAsync(_pageSize, cancellationToken);

                var filters = new Dictionary<String, String>();

                IEnumerable<(String ApiName, IAsyncEnumerable<DynamicDataSet> Pages)> sources = OperationalApis
                    .Select(apiName => (ApiName: apiName, Pages: _externalDataGateway.FetchDataPagedAsync(apiName, filters, cancellationToken)));

                await using var pageRounds = new MergedPageEnumerator(sources, cancellationToken);

                IReadOnlyDictionary<String, DynamicDataSet>? round;
                while ((round = await pageRounds.MoveNextRoundAsync()) is not null)
                {
                    pageNumber++;
                    cancellationToken.ThrowIfCancellationRequested();

                    Boolean roundHasData = round.Values.Any(dataSet => dataSet.Rows.Count > 0);
                    if (!roundHasData)
                        break; // Ninguna API aportó datos en esta ronda: no queda nada más por procesar.

                    foreach (var (apiName, page) in round)
                    {
                        result.ExtractedRecordsByApi[apiName] = result.ExtractedRecordsByApi.GetValueOrDefault(apiName) + page.Rows.Count;

                        if (page.Rows.Count > 0)
                            _logger.LogInformation("Extract [{Api}] página {Page}: {Count} registros.", apiName, pageNumber, page.Rows.Count);
                    }

                    // ---------- Transform ----------
                    DynamicDataSet unifiedPage = _merger.Merge(
                        OperationalApis.Select(apiName => round[apiName]),
                        joinField: ExternalDataFields.DocumentNumber,
                        joinType: DataSetJoinType.FullOuter);

                    IReadOnlyList<ApplicationDataSheet> sheets = _mappingService.Map(unifiedPage);
                    result.TransformedRecords += sheets.Count;

                    // ---------- Load [+ detección/notificación de cambios, si no es migración inicial] + avance de etl_job_control, en la misma transacción de esta ronda ----------
                    Int32 loaded = await EtlTransactionHelper.RunInOwnTransactionAsync(_unitOfWork, async uow =>
                    {
                        var repository = uow.GetRepository<IApplicationDataSheetRepository>();

                        if (isInitialMigration)
                        {
                            // Migración inicial: solo carga, sin snapshot "antes", sin detección ni
                            // notificación de cambios. Se actualiza el avance en AMBOS registros de
                            // control: el del job "application_data_sheet" (como siempre) y el del
                            // job "application_data_sheet_migration" (para poder ver el progreso de
                            // la migración en etl_job_control mientras corre).
                            Int32 insertedInitial = await repository.UpsertBatchAsync(sheets, cancellationToken);
                            await jobControl.RegisterPageProgressAsync(pageNumber, insertedInitial, cancellationToken);
                            await migrationJobControl.RegisterPageProgressAsync(pageNumber, insertedInitial, cancellationToken);
                            return insertedInitial;
                        }

                        // "Antes": Id/Estado/Comentario/FechaComentario tal como están HOY en la tabla,
                        // acotado a los documentos de esta ronda - se consulta ANTES del upsert porque
                        // el upsert sobreescribe Estado/Comentario/FechaComentario con el valor "después"
                        // (el "Id" BIGSERIAL no lo toca el upsert, así que sigue siendo válido luego).
                        var documentNumbers = sheets.Select(s => s.DocumentoTransporteHbl).ToList();
                        var previousSnapshots = await repository.GetChangeSnapshotsAsync(documentNumbers, cancellationToken);

                        var changes = _changeDetector.DetectChanges(sheets, previousSnapshots);

                        Int32 affected = await repository.UpsertBatchAsync(sheets, cancellationToken);

                        if (changes.Count > 0)
                        {
                            // Documentos NUEVOS (sin snapshot "antes"): su Id de application_data_sheet
                            // recién se generó en el upsert de arriba, así que se resuelve aquí con una
                            // consulta adicional acotada solo a esos documentos (no a toda la página).
                            var newDocuments = changes.Where(c => c.IsNewDocument).Select(c => c.DocumentoTransporteHbl).ToList();
                            if (newDocuments.Count > 0)
                            {
                                var newDocumentIds = await repository.GetIdsByDocumentAsync(newDocuments, cancellationToken);
                                foreach (var change in changes)
                                {
                                    if (change.IsNewDocument && newDocumentIds.TryGetValue(change.DocumentoTransporteHbl, out Int64 newId))
                                        change.IdOperacion = newId;
                                }
                            }

                            await changeNotifier.NotifyChangesAsync(changes, cancellationToken);

                            result.StateChangesDetected += changes.Count(c => c.StateChanged);
                            result.CommentChangesDetected += changes.Count(c => c.CommentChanged);

                            _logger.LogInformation(
                                "Página {Page}: {StateChanges} cambio(s) de estado y {CommentChanges} cambio(s) de comentario detectados ({NewDocuments} documento(s) nuevo(s)).",
                                pageNumber, changes.Count(c => c.StateChanged), changes.Count(c => c.CommentChanged), newDocuments.Count);
                        }

                        await jobControl.RegisterPageProgressAsync(pageNumber, affected, cancellationToken);

                        return affected;
                    }, cancellationToken);

                    result.LoadedRecords += loaded;

                    _logger.LogInformation(
                        "Página {Page}: {Transformed} filas unificadas, {Loaded} cargadas en connection360write.application_data_sheet (transacción propia).",
                        pageNumber, sheets.Count, loaded);
                }

                // Todas las páginas/rondas cargaron sin error: recién ahora se marca COMPLETED.
                await jobControl.CompleteAsync(cancellationToken);

                if (isInitialMigration)
                {
                    // La migración inicial completó sin error: se marca COMPLETED para siempre - las
                    // corridas siguientes ya harán la validación completa de cambios.
                    await migrationJobControl.CompleteAsync(cancellationToken);
                    _logger.LogInformation(
                        "Migración inicial de connection360write.application_data_sheet completada (job 'application_data_sheet_migration'): las próximas corridas ya validarán cambios.");
                }

                result.IsInitialMigrationRun = isInitialMigration;
                result.Success = true;
                _logger.LogInformation(
                    "Corrida ETL finalizada. Éxito={Success}. MigraciónInicial={IsInitialMigration}. Páginas procesadas={Pages}. Transformados={Transformed}. Cargados={Loaded}. Cambios de estado={StateChanges}. Cambios de comentario={CommentChanges}. Duración={Duration}.",
                    result.Success, result.IsInitialMigrationRun, pageNumber, result.TransformedRecords, result.LoadedRecords, result.StateChangesDetected, result.CommentChangesDetected, result.Duration);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "La corrida del proceso ETL terminó con error.");

                try
                {
                    await jobControl.FailAsync(CancellationToken.None);
                }
                catch (Exception failEx)
                {
                    _logger.LogError(failEx, "No se pudo marcar el registro de etl_job_control como FAILED.");
                }

                try
                {
                    // No-op si migrationJobControl.StartAsync nunca llegó a llamarse (ver
                    // EtlJobControlTracker.FailAsync): seguro llamarlo incondicionalmente.
                    await migrationJobControl.FailAsync(CancellationToken.None);
                }
                catch (Exception failEx)
                {
                    _logger.LogError(failEx, "No se pudo marcar el registro de migración inicial (etl_job_control) como FAILED.");
                }
            }
            finally
            {
                result.FinishedAtUtc = DateTime.UtcNow;
            }

            return result;
        }
    }
}
