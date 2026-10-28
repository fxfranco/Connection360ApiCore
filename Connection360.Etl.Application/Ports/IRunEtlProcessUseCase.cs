using Connection360.Etl.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace Connection360.Etl.Application.Ports
{
    /// <summary>
    /// Puerto primario (entrante) del proceso ETL completo. Lo invoca Connection360.Etl.App
    /// (composition root / punto de entrada del ejecutable).
    /// </summary>
    public interface IRunEtlProcessUseCase
    {
        /// <summary>Ejecuta una corrida completa de Extract -> Transform -> Load.</summary>
        Task<EtlRunResult> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
