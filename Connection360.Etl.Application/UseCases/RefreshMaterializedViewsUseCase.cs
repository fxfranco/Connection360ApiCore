using Connection360.Etl.Application.DTOs;
using Connection360.Etl.Application.Ports;
using Connection360.Etl.Domain.Enums;
using Connection360.Etl.Domain.Ports.Persistence;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Connection360.Etl.Application.UseCases
{
    /// <summary>
    /// Sincroniza las vistas materializadas de connection360read con lo que cargó la ETL.
    /// <list type="bullet">
    /// <item><description><b>Después de la carga:</b> se invoca cuando todos los procesos ETL ya terminaron, así que no compite con ellos.</description></item>
    /// <item><description><b>Sin bloquear lecturas:</b> cada vista se refresca con REFRESH MATERIALIZED VIEW CONCURRENTLY (ver <see cref="IMaterializedViewRepository"/>).</description></item>
    /// <item><description><b>Aislada:</b> no usa transacción (cada refresco se confirma solo) y una vista que falla no impide refrescar las demás; nunca lanza.</description></item>
    /// <item><description><b>Secuencial:</b> una vista a la vez, para limitar la carga sobre PostgreSQL.</description></item>
    /// </list>
    /// No depende de la configuración de Infrastructure: recibe ya resuelto el indicador
    /// <c>enabled</c> (lo conecta con el appsettings la composición de dependencias de Etl.App).
    /// </summary>
    public class RefreshMaterializedViewsUseCase : IRefreshMaterializedViewsUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly Boolean _enabled;
        private readonly ILogger<RefreshMaterializedViewsUseCase> _logger;

        public RefreshMaterializedViewsUseCase(IUnitOfWork unitOfWork, Boolean enabled, ILogger<RefreshMaterializedViewsUseCase> logger)
        {
            _unitOfWork = unitOfWork;
            _enabled = enabled;
            _logger = logger;
        }

        public async Task<MaterializedViewsRefreshResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var result = new MaterializedViewsRefreshResult { StartedAtUtc = DateTime.UtcNow };

            try
            {
                if (!_enabled)
                {
                    result.Skipped = true;
                    _logger.LogInformation("Sincronización de vistas materializadas deshabilitada por configuración: no se refresca ninguna vista.");
                    return result;
                }

                IMaterializedViewRepository repository = _unitOfWork.GetRepository<IMaterializedViewRepository>();

                foreach (MaterializedViewName view in Enum.GetValues<MaterializedViewName>())
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        result.Views.Add(new MaterializedViewRefreshOutcome(view, false, TimeSpan.Zero, "Cancelado antes de refrescar la vista."));
                        _logger.LogWarning("Refresco de la vista materializada {View} omitido: la corrida fue cancelada.", view);
                        continue;
                    }

                    long start = Stopwatch.GetTimestamp();
                    try
                    {
                        await repository.RefreshConcurrentlyAsync(view, cancellationToken);
                        TimeSpan elapsed = Stopwatch.GetElapsedTime(start);
                        result.Views.Add(new MaterializedViewRefreshOutcome(view, true, elapsed));
                        if (_logger.IsEnabled(LogLevel.Information))
                        {
                            _logger.LogInformation("Vista materializada {View} refrescada en {Duration}.", view, elapsed);
                        }
                    }
                    catch (Exception ex)
                    {
                        TimeSpan elapsed = Stopwatch.GetElapsedTime(start);
                        result.Views.Add(new MaterializedViewRefreshOutcome(view, false, elapsed, ex.Message));
                        _logger.LogError(ex, "No se pudo refrescar la vista materializada {View}; se continúa con las demás.", view);
                    }
                }
            }
            catch (Exception ex)
            {
                // Defensa final (p. ej. no se pudo resolver el repositorio): el contrato es no lanzar.
                _logger.LogError(ex, "La sincronización de vistas materializadas terminó con error.");
                foreach (MaterializedViewName view in Enum.GetValues<MaterializedViewName>().Where(candidate => !result.Views.Exists(v => v.View == candidate)))
                {
                    result.Views.Add(new MaterializedViewRefreshOutcome(view, false, TimeSpan.Zero, ex.Message));
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
