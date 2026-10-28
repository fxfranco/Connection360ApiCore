using Connection360.Etl.Domain.Ports.Persistence;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Connection360.Etl.Application.UseCases
{
    /// <summary>
    /// Helper compartido para el paso "Load": ejecuta una operación de persistencia dentro de SU
    /// PROPIA transacción (Begin -&gt; operación -&gt; Commit, o Rollback si falla). Extraído de
    /// RunEtlProcessUseCase y RunLogsEtlProcessUseCase para no duplicar este mismo bloque
    /// try/Begin/Commit/Rollback: con el proceso paginado, cada página se carga en una transacción
    /// independiente.
    /// <para>
    /// La acción recibe el propio <see cref="IUnitOfWork"/> (en vez de un único
    /// <c>TRepository</c> ya resuelto) porque, con el registro de avance en
    /// connection360write.etl_job_control, cada página necesita resolver DOS repositorios dentro de
    /// la misma transacción: el repositorio de datos (Load) y <see cref="IEtlJobControlRepository"/>
    /// (para que el progreso de esa página se confirme o se revierta junto con sus datos).
    /// </para>
    /// </summary>
    internal static class EtlTransactionHelper
    {
        public static async Task<TResult> RunInOwnTransactionAsync<TResult>(
            IUnitOfWork unitOfWork,
            Func<IUnitOfWork, Task<TResult>> action,
            CancellationToken cancellationToken = default)
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                TResult resultValue = await action(unitOfWork);
                await unitOfWork.CommitAsync(cancellationToken);
                return resultValue;
            }
            catch
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
