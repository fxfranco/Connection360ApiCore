using System;

namespace Connection360.Etl.Infrastructure.Configuration
{
    /// <summary>
    /// Configuración (appsettings, sección "MaterializedViews") de la sincronización de las vistas
    /// materializadas de connection360read al finalizar los procesos ETL.
    /// </summary>
    public class MaterializedViewSettings
    {
        /// <summary>Nombre de la sección donde está la configuración.</summary>
        public const String SectionName = "MaterializedViews";

        /// <summary>Si es false, la ETL no refresca ninguna vista materializada.</summary>
        public Boolean RefreshEnabled { get; set; } = true;

        /// <summary>
        /// Tiempo máximo (segundos) de cada REFRESH. Un refresco sobre tablas grandes puede superar
        /// el timeout por defecto de 30 s de Dapper/Npgsql. 0 = sin límite.
        /// </summary>
        public Int32 CommandTimeoutSeconds { get; set; } = 600;
    }
}
