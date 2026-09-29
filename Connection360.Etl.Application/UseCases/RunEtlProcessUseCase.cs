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
    /// <item><description><b>Load</b>: persiste el resultado en PostgreSQL (connection360write.application_data_sheet), vía <see cref="IUnitOfWork"/> / <see cref="IApplicationDataSheetRepository"/>.</description></item>
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
        private readonly ILogger<RunEtlProcessUseCase> _logger;

        /// <param name="pageSize">
        /// Tamaño de página GLOBAL configurado para las 5 APIs operativas (ExternalApiSettings.PageSize),
        /// ya resuelto por la composición de dependencias de Connection360.Etl.App - null si la
        /// paginación está deshabilitada. Se pasa como un valor plano (no como el objeto de
        /// configuración de Infrastructure) para no romper la arquitectura hexagonal: Application no
        /// puede depender de Infrastructure.
        /// </param>
        public RunEtlProcessUseCase(
            IExternalDataGateway externalDataGateway,
            IDynamicDataSetMerger merger,
            IShipmentsDataSheetMappingService mappingService,
            IUnitOfWork unitOfWork,
            Int32? pageSize,
            ILogger<RunEtlProcessUseCase> logger)
        {
            _externalDataGateway = externalDataGateway;
            _merger = merger;
            _mappingService = mappingService;
            _unitOfWork = unitOfWork;
            _pageSize = pageSize;
            _logger = logger;
        }

        public async Task<EtlRunResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var result = new EtlRunResult { StartedAtUtc = DateTime.UtcNow };
            Int32 pageNumber = 0;

            var jobControlRepository = _unitOfWork.GetRepository<IEtlJobControlRepository>();
            var jobControl = new EtlJobControlTracker(jobControlRepository, EtlJobName.ApplicationDataSheet);

            try
            {
                _logger.LogInformation("Iniciando corrida del proceso ETL.");

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

                    // ---------- Load + avance de etl_job_control, en la misma transacción de esta ronda ----------
                    Int32 loaded = await EtlTransactionHelper.RunInOwnTransactionAsync(_unitOfWork, async uow =>
                    {
                        var repository = uow.GetRepository<IApplicationDataSheetRepository>();
                        Int32 affected = await repository.UpsertBatchAsync(sheets, cancellationToken);

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

                result.Success = true;
                _logger.LogInformation(
                    "Corrida ETL finalizada. Éxito={Success}. Páginas procesadas={Pages}. Transformados={Transformed}. Cargados={Loaded}. Duración={Duration}.",
                    result.Success, pageNumber, result.TransformedRecords, result.LoadedRecords, result.Duration);
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
            }
            finally
            {
                result.FinishedAtUtc = DateTime.UtcNow;
            }

            return result;
        }
    }
}
