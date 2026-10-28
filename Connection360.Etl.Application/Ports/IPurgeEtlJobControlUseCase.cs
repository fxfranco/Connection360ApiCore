using System;
using System.Threading;
using System.Threading.Tasks;

namespace Connection360.Etl.Application.Ports
{
    /// <summary>
    /// Puerto primario (entrante) del proceso de depuración de connection360write.etl_job_control:
    /// elimina los registros viejos del job "application_data_sheet" (nunca los de
    /// "log_status_tracking"). Es un proceso independiente de los dos procesos ETL - lo invoca
    /// Connection360.Etl.App después de que ambos terminan.
    /// </summary>
    public interface IPurgeEtlJobControlUseCase
    {
        /// <summary>Ejecuta la depuración. Nunca lanza: si falla, se registra en el log y devuelve 0.</summary>
        /// <returns>Cantidad de registros eliminados.</returns>
        Task<Int32> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
