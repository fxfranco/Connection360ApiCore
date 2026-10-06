using Connection360.Etl.Orchestrator.Configuration;

namespace Connection360.Etl.Orchestrator.Test.TestHelpers
{
    /// <summary>Configuraciones de procesos hijo reales, baratos y portables (Windows/Linux/macOS) para probar ExternalProcessRunner.</summary>
    internal static class ChildProcesses
    {
        /// <summary>Ruta del host "dotnet" que está ejecutando las pruebas (o "dotnet" del PATH si no se puede deducir).</summary>
        public static String DotnetExecutable
        {
            get
            {
                String? current = Environment.ProcessPath;
                return current is not null && Path.GetFileNameWithoutExtension(current).Equals("dotnet", StringComparison.OrdinalIgnoreCase)
                    ? current
                    : "dotnet";
            }
        }

        public static CronJobSettings Dotnet(String arguments, Int32? timeoutMinutes = null) => new()
        {
            Name = "dotnet-job",
            CronExpression = "* * * * *",
            ExecutablePath = DotnetExecutable,
            Arguments = arguments,
            WorkingDirectory = AppContext.BaseDirectory,
            TimeoutMinutes = timeoutMinutes,
        };

        /// <summary>Proceso que duerme ~30 segundos: siempre hay que matarlo; nunca se espera a que termine solo.</summary>
        public static CronJobSettings LongRunning(Int32? timeoutMinutes = null) => new()
        {
            Name = "sleeper",
            CronExpression = "* * * * *",
            ExecutablePath = OperatingSystem.IsWindows() ? "cmd.exe" : "sleep",
            Arguments = OperatingSystem.IsWindows() ? "/c ping -n 30 127.0.0.1 >nul" : "30",
            WorkingDirectory = AppContext.BaseDirectory,
            TimeoutMinutes = timeoutMinutes,
        };
    }
}
