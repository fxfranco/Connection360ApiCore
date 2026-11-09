namespace Connection360.Etl.Orchestrator.Configuration
{
    /// <summary>Raíz de la sección "Orchestrator" del appsettings.</summary>
    public sealed class OrchestratorSettings
    {
        public const String SectionName = "Orchestrator";

        /// <summary>
        /// Trabajos programados. Hoy solo existe uno (la ETL principal, Connection360.Etl.App),
        /// pero el orquestador soporta cualquier cantidad sin cambios de código: cada uno nuevo es
        /// simplemente una entrada más acá.
        /// </summary>
        public List<CronJobSettings> CronJobs { get; set; } = new();
    }
}
