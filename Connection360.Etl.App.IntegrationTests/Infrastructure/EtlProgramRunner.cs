using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Connection360.Etl.App.IntegrationTests.Infrastructure
{
    /// <summary>Resultado de ejecutar el Program.cs real: código de salida, excepción no controlada (si la hubo) y logs.</summary>
    public sealed record ProgramRun(Int32 ExitCode, Exception? Exception, ListLoggerProvider Logs);

    /// <summary>
    /// Ejecuta el punto de entrada REAL de Connection360.Etl.App (los top-level statements de
    /// Program.cs, que es internal y no se puede referenciar) en el mismo proceso de pruebas.
    /// <para>
    /// Como Program.cs construye su propio <c>Host.CreateApplicationBuilder</c>, la única forma de
    /// sustituir los bordes (BD, HTTP, casos de uso) sin modificar producción es la misma que usa
    /// WebApplicationFactory: escuchar el DiagnosticListener "Microsoft.Extensions.Hosting", que
    /// publica el evento <c>HostBuilding</c> con un IHostBuilder justo antes de construir el host, y
    /// registrar ahí servicios adicionales (que se aplican DESPUÉS de los de Program.cs).
    /// </para>
    /// Usa estado global del proceso (Environment.ExitCode), por lo que las pruebas que lo usan no
    /// corren en paralelo (ver AssemblyInfo.cs).
    /// </summary>
    public static class EtlProgramRunner
    {
        private static readonly Assembly AppAssembly = typeof(Connection360.Etl.App.Extensions.ServiceCollectionExtensions).Assembly;

        public static ProgramRun Run(Action<IServiceCollection> configureServices, params String[] extraArgs)
        {
            var logs = new ListLoggerProvider();

            // Entorno y carpeta de contenido explícitos: no dependen de DOTNET_ENVIRONMENT ni del directorio actual.
            var args = new List<String> { "--environment=Production", $"--contentRoot={AppContext.BaseDirectory}" };
            args.AddRange(extraArgs);

            Environment.ExitCode = 0;
            Exception? failure = null;

            using (DiagnosticListener.AllListeners.Subscribe(new ListenerObserver(services =>
            {
                services.AddSingleton<ILoggerProvider>(logs);
                configureServices(services);
            })))
            {
                try
                {
                    AppAssembly.EntryPoint!.Invoke(null, new Object[] { args.ToArray() });
                }
                catch (TargetInvocationException ex)
                {
                    failure = ex.InnerException;
                }
            }

            Int32 exitCode = Environment.ExitCode;
            Environment.ExitCode = 0; // no contaminar el código de salida del proceso de pruebas
            return new ProgramRun(exitCode, failure, logs);
        }

        private sealed class ListenerObserver : IObserver<DiagnosticListener>
        {
            private readonly Action<IServiceCollection> _configure;

            public ListenerObserver(Action<IServiceCollection> configure)
            {
                _configure = configure;
            }

            public void OnNext(DiagnosticListener listener)
            {
                if (listener.Name == "Microsoft.Extensions.Hosting")
                    listener.Subscribe(new HostingEventObserver(_configure));
            }

            public void OnCompleted() { }
            public void OnError(Exception error) { }
        }

        private sealed class HostingEventObserver : IObserver<KeyValuePair<String, Object?>>
        {
            private readonly Action<IServiceCollection> _configure;

            public HostingEventObserver(Action<IServiceCollection> configure)
            {
                _configure = configure;
            }

            public void OnNext(KeyValuePair<String, Object?> value)
            {
                if (value.Key == "HostBuilding" && value.Value is IHostBuilder hostBuilder)
                    hostBuilder.ConfigureServices((_, services) => _configure(services));
            }

            public void OnCompleted() { }
            public void OnError(Exception error) { }
        }
    }
}
