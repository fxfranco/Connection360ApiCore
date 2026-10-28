using Connection360.Etl.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Connection360.Etl.Application.DTOs
{
    /// <summary>Resultado del refresco de una vista materializada.</summary>
    public sealed class MaterializedViewRefreshOutcome
    {
        public MaterializedViewRefreshOutcome(MaterializedViewName view, Boolean success, TimeSpan duration, String? errorMessage = null)
        {
            View = view;
            Success = success;
            Duration = duration;
            ErrorMessage = errorMessage;
        }

        public MaterializedViewName View { get; }
        public Boolean Success { get; }
        public TimeSpan Duration { get; }
        public String? ErrorMessage { get; }
    }

    /// <summary>
    /// Resultado de la sincronización de las vistas materializadas que corre al finalizar los
    /// procesos ETL (ver <see cref="Ports.IRefreshMaterializedViewsUseCase"/>).
    /// </summary>
    public sealed class MaterializedViewsRefreshResult
    {
        /// <summary>true si la sincronización está deshabilitada por configuración (no se tocó ninguna vista).</summary>
        public Boolean Skipped { get; set; }

        /// <summary>Detalle por vista, en el orden en que se refrescaron.</summary>
        public List<MaterializedViewRefreshOutcome> Views { get; } = new();

        public DateTime StartedAtUtc { get; set; }
        public DateTime FinishedAtUtc { get; set; }

        /// <summary>true cuando ninguna vista falló (también si se omitió por configuración).</summary>
        public Boolean Success => Views.All(v => v.Success);

        public TimeSpan Duration => FinishedAtUtc - StartedAtUtc;
    }
}
