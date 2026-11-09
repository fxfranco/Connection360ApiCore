using Connection360.Etl.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace Connection360.Etl.Application.Ports
{
    /// <summary>
    /// Puerto primario (entrante) de la sincronización de las vistas materializadas del esquema de
    /// lectura (connection360read) con los datos que cargaron los procesos ETL. Lo invoca
    /// Connection360.Etl.App DESPUÉS de que terminan todos los procesos ETL (para no competir por
    /// recursos de base de datos con la carga principal) y refresca cada vista sin bloquear a los
    /// lectores (REFRESH ... CONCURRENTLY).
    /// </summary>
    public interface IRefreshMaterializedViewsUseCase
    {
        /// <summary>
        /// Refresca las vistas, una a una. Nunca lanza: si una vista falla se registra, se sigue con
        /// las demás y el resultado indica cuáles fallaron.
        /// </summary>
        Task<MaterializedViewsRefreshResult> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
