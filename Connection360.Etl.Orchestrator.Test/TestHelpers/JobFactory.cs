using Connection360.Etl.Orchestrator.Configuration;

namespace Connection360.Etl.Orchestrator.Test.TestHelpers
{
    internal static class JobFactory
    {
        /// <summary>
        /// Trabajo válido por defecto. La expresión "0 0 1 * *" (día 1 de cada mes) garantiza que la
        /// próxima ejecución quede a lo sumo a ~31 días, así los bucles solo esperan y la prueba los cancela.
        /// </summary>
        public static CronJobSettings Valid(String name = "job1", String cron = "0 0 1 * *") => new()
        {
            Name = name,
            Enabled = true,
            CronExpression = cron,
            TimeZoneId = "UTC",
            ExecutablePath = "dotnet",
            Arguments = "app.dll",
            WorkingDirectory = ".",
        };
    }
}
