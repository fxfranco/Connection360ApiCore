using Connection360.Etl.Application.DTOs;
using Connection360.Etl.Application.Ports;
using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Domain.Enums;
using Connection360.Etl.Domain.Interfaces;
using Connection360.Etl.Domain.Ports.Persistence;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Connection360.Etl.Application.UseCases
{
    /// <summary>
    /// Orquesta una corrida del proceso ETL de logs:
    /// <list type="number">
    /// <item><description><b>Extract</b>: consulta únicamente la API "DATALOGS" (histórico de cambios de estado), vía <see cref="IExternalDataGateway"/>.</description></item>
    /// <item><description><b>Transform</b>: convierte cada registro en una fila de <see cref="LogStatusTracking"/> con <see cref="ILogStatusMappingService"/>. No hay combinación (merge) con ningún otro dataset.</description></item>
    /// <item><description><b>Load</b>: inserta el resultado en PostgreSQL (connection360write.log_status_tracking), vía <see cref="IUnitOfWork"/> / <see cref="ILogStatusTrackingRepository"/>.</description></item>
    /// </list>
    /// Este flujo es completamente independiente de <see cref="RunEtlProcessUseCase"/>: no comparte
    /// dataset, mapeo ni repositorio con él, y su éxito o fracaso no afecta lo ya cargado por aquel.
    /// <para>
    /// <b>Ejecución única (connection360write.etl_job_control, job "log_status_tracking"):</b> a
    /// diferencia de <see cref="RunEtlProcessUseCase"/> (que siempre corre), este proceso NO debe
    /// repetirse una vez que ya completó exitosamente: si ya existe un registro COMPLETED para este
    /// job, <see cref="ExecuteAsync"/> no hace nada. Si no existe, se registra un nuevo renglón en
    /// PROCESSING (autocommit, para que sobreviva aunque una página falle), cada página actualiza su
    /// avance (last_processed_page, total_records_processed) DENTRO de la misma transacción que su
    /// carga, y solo después de que la última página cargó sin error se marca COMPLETED. Si algo
    /// falla, el registro queda en FAILED en vez de reintentarse solo.
    /// </para>
    /// <para>
    /// <b>Paginación:</b> al ser una sola fuente (a diferencia de RunEtlProcessUseCase, que combina
    /// 5 APIs y necesita sincronizar sus páginas en rondas), aquí basta con recorrer directamente la
    /// secuencia paginada de <c>IExternalDataGateway.FetchDataPagedAsync</c>. Con la paginación
    /// deshabilitada, esa secuencia entrega una sola página con el 100% de los datos, reproduciendo
    /// el comportamiento de una sola extracción/transformación/carga.
    /// </para>
    /// </summary>
    public class RunLogsEtlProcessUseCase : IRunLogsEtlProcessUseCase
    {
        // Única API que consume este proceso: el histórico de cambios de estado.
        private const String LogsApi = "DATALOGS";

        private readonly IExternalDataGateway _externalDataGateway;
        private readonly ILogStatusMappingService _mappingService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly Int32? _pageSize;
        private readonly ILogger<RunLogsEtlProcessUseCase> _logger;

        /// <param name="pageSize">
        /// Tamaño de página GLOBAL configurado (ExternalApiSettings.PageSize), ya resuelto por la
        /// composición de dependencias de Connection360.Etl.App - null si la paginación está
        /// deshabilitada. Mismo valor que usa RunEtlProcessUseCase para las 5 APIs operativas.
        /// </param>
        public RunLogsEtlProcessUseCase(
            IExternalDataGateway externalDataGateway,
            ILogStatusMappingService mappingService,
            IUnitOfWork unitOfWork,
            Int32? pageSize,
            ILogger<RunLogsEtlProcessUseCase> logger)
        {
            _externalDataGateway = externalDataGateway;
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
            var jobControl = new EtlJobControlTracker(jobControlRepository, EtlJobName.LogStatusTracking);

            try
            {
                // Garantiza que este proceso se ejecute una sola vez: si ya hay una corrida COMPLETED
                // registrada para este job, no se vuelve a ejecutar.
                if (await jobControl.HasCompletedRunAsync(cancellationToken))
                {
                    _logger.LogInformation("El proceso ETL de logs (DATALOGS) ya se ejecutó exitosamente antes (etl_job_control); no se vuelve a ejecutar.");
                    result.Success = true;
                    return result;
                }

                _logger.LogInformation("Iniciando corrida del proceso ETL de logs (DATALOGS).");

                // Registro de control en PROCESSING (autocommit): sobrevive aunque una página falle
                // más adelante, para poder marcarlo como FAILED y saber en qué página quedó.
                await jobControl.StartAsync(_pageSize, cancellationToken);

                var filters = new Dictionary<String, String>();

                await foreach (DynamicDataSet page in _externalDataGateway.FetchDataPagedAsync(LogsApi, filters, cancellationToken))
                {
                    pageNumber++;
                    cancellationToken.ThrowIfCancellationRequested();

                    // ---------- Extract ----------
                    result.ExtractedRecordsByApi[LogsApi] = result.ExtractedRecordsByApi.GetValueOrDefault(LogsApi) + page.Rows.Count;
                    _logger.LogInformation("Extract [{Api}] página {Page}: {Count} registros obtenidos.", LogsApi, pageNumber, page.Rows.Count);

                    // ---------- Transform ----------
                    IReadOnlyList<LogStatusTracking> logRows = _mappingService.Map(page);
                    result.TransformedRecords += logRows.Count;

                    // ---------- Load + avance de etl_job_control, en la misma transacción de esta página ----------
                    Int32 loaded = await EtlTransactionHelper.RunInOwnTransactionAsync(_unitOfWork, async uow =>
                    {
                        var repository = uow.GetRepository<ILogStatusTrackingRepository>();
                        Int32 affected = await repository.InsertBatchAsync(logRows, cancellationToken);

                        await jobControl.RegisterPageProgressAsync(pageNumber, affected, cancellationToken);

                        return affected;
                    }, cancellationToken);

                    result.LoadedRecords += loaded;

                    _logger.LogInformation(
                        "Página {Page}: {Transformed} filas de log, {Loaded} insertadas en connection360write.log_status_tracking (transacción propia).",
                        pageNumber, logRows.Count, loaded);
                }

                // Todas las páginas cargaron sin error: recién ahora se marca COMPLETED.
                await jobControl.CompleteAsync(cancellationToken);

                result.Success = true;
                _logger.LogInformation(
                    "Corrida ETL de logs finalizada. Éxito={Success}. Páginas procesadas={Pages}. Transformados={Transformed}. Cargados={Loaded}. Duración={Duration}.",
                    result.Success, pageNumber, result.TransformedRecords, result.LoadedRecords, result.Duration);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "La corrida del proceso ETL de logs terminó con error.");

                try
                {
                    // CancellationToken.None a propósito: el registro de FAILED debe quedar incluso
                    // si la falla fue por cancelación (Ctrl+C).
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
