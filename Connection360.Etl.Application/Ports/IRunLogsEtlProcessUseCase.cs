using Connection360.Etl.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace Connection360.Etl.Application.Ports
{
    /// <summary>
    /// Puerto primario (entrante) del proceso ETL de logs (API "DATALOGS" -&gt;
    /// connection360write.log_status_tracking). Totalmente independiente de
    /// <see cref="IRunEtlProcessUseCase"/> (bodega de datos de envíos): tiene su propia extracción,
    /// su propio mapeo y su propio repositorio. Lo invoca Connection360.Etl.App después de que
    /// finaliza la corrida del proceso ETL principal.
    /// </summary>
    public interface IRunLogsEtlProcessUseCase
    {
        /// <summary>Ejecuta una corrida completa de Extract -> Transform -> Load del histórico de logs.</summary>
        Task<EtlRunResult> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
