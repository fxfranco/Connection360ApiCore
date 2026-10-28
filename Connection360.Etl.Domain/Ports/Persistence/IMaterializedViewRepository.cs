using Connection360.Etl.Domain.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace Connection360.Etl.Domain.Ports.Persistence
{
    /// <summary>
    /// Puerto secundario (saliente): sincroniza las vistas materializadas del esquema de lectura
    /// (connection360read). Lo implementa Connection360.Etl.Infrastructure.
    /// </summary>
    public interface IMaterializedViewRepository
    {
        /// <summary>
        /// Refresca la vista SIN bloquear las lecturas (REFRESH MATERIALIZED VIEW CONCURRENTLY): las
        /// consultas siguen leyendo la versión anterior mientras se calcula la nueva. Requiere que
        /// la vista ya esté poblada y tenga un índice único (los scripts de Documents lo crean). Se
        /// ejecuta fuera de cualquier transacción de la ETL (cada refresco se confirma por sí solo).
        /// </summary>
        Task RefreshConcurrentlyAsync(MaterializedViewName view, CancellationToken cancellationToken = default);
    }
}
