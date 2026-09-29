using Connection360.Etl.Application.Ports;
using Connection360.Etl.Domain.Enums;
using Connection360.Etl.Domain.Ports.Persistence;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Connection360.Etl.Application.UseCases
{
    /// <summary>
    /// Proceso independiente de depuración de connection360write.etl_job_control: elimina, para el
    /// job "application_data_sheet" únicamente, los registros cuyo updated_at sea anterior a
    /// <see cref="_applicationDataSheetRetentionDays"/> días. Los registros de "log_status_tracking"
    /// NUNCA se depuran (ese job se ejecuta una sola vez, así que no tiene sentido acumular corridas).
    /// <para>
    /// No depende directamente de Connection360.Etl.Infrastructure.Configuration.EtlJobControlSettings
    /// (violaría la arquitectura hexagonal: Application no puede depender de Infrastructure) - recibe
    /// la cantidad de días ya resuelta como un <see cref="Int32"/> plano; quien conecta ese valor con
    /// el appsettings es la composición de dependencias de Connection360.Etl.App (ver
    /// Extensions/ServiceCollectionExtensions.cs), no este caso de uso.
    /// </para>
    /// </summary>
    public class PurgeEtlJobControlUseCase : IPurgeEtlJobControlUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly Int32 _applicationDataSheetRetentionDays;
        private readonly ILogger<PurgeEtlJobControlUseCase> _logger;

        public PurgeEtlJobControlUseCase(IUnitOfWork unitOfWork, Int32 applicationDataSheetRetentionDays, ILogger<PurgeEtlJobControlUseCase> logger)
        {
            _unitOfWork = unitOfWork;
            _applicationDataSheetRetentionDays = applicationDataSheetRetentionDays;
            _logger = logger;
        }

        public async Task<Int32> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<IEtlJobControlRepository>();

                Int32 deleted = await repository.DeleteOlderThanAsync(
                    EtlJobName.ApplicationDataSheet,
                    _applicationDataSheetRetentionDays,
                    cancellationToken);

                _logger.LogInformation(
                    "Depuración de etl_job_control ({JobName}): {Deleted} registro(s) eliminados (updated_at anterior a {Days} días).",
                    EtlJobName.ApplicationDataSheet.ToDbValue(), deleted, _applicationDataSheetRetentionDays);

                return deleted;
            }
            catch (Exception ex)
            {
                // Es un proceso de housekeeping independiente: una falla acá no debe afectar el
                // resultado (Success/ExitCode) de los procesos ETL que sí importan.
                _logger.LogError(ex, "La depuración de etl_job_control terminó con error.");
                return 0;
            }
        }
    }
}
