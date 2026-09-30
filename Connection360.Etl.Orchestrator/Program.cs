using Connection360.Etl.Orchestrator.Configuration;
using Connection360.Etl.Orchestrator.Execution;
using Connection360.Etl.Orchestrator.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// ---------- Composition root del orquestador ----------
// A diferencia de Connection360.Etl.App (que corre UNA sola vez y termina, pensado para que un
// scheduler externo lo invoque), este proceso es un host de LARGA DURACIÓN: se queda corriendo en
// primer plano -la "pantalla" que pide el usuario- programando y lanzando Connection360.Etl.App (o
// cualquier otro ejecutable que se agregue a "Orchestrator:CronJobs" en el appsettings) según sus
// expresiones cron, hasta que se cierre con Ctrl+C o el sistema operativo lo detenga.
//
// Este proyecto NO tiene ninguna referencia de proyecto a Connection360.Etl.App ni a sus capas: lo
// trata como una caja negra externa (un proceso más que lanzar con System.Diagnostics.Process),
// exactamente igual que lo haría el Programador de tareas de Windows o un cron del sistema
// operativo, solo que centralizando los logs de todas las corridas en una sola pantalla.
var builder = Host.CreateApplicationBuilder(args);

// Reemplaza el logger de consola por defecto por uno de una sola línea con timestamp: es lo que
// convierte esta consola en la "pantalla" solicitada, donde se ven en vivo tanto los mensajes del
// propio orquestador (próxima ejecución, resultado de cada corrida, etc.) como -línea por línea-
// todo lo que imprime internamente cada corrida de la ETL.
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    options.SingleLine = true;
});

builder.Services.Configure<OrchestratorSettings>(builder.Configuration.GetSection(OrchestratorSettings.SectionName));
builder.Services.AddSingleton<IExternalProcessRunner, ExternalProcessRunner>();
builder.Services.AddHostedService<OrchestratorHostedService>();

using IHost host = builder.Build();

Console.WriteLine("==================================================================");
Console.WriteLine(" Connection360 ETL Orchestrator");
Console.WriteLine(" Presione Ctrl+C para detener. Los logs de cada corrida de la ETL");
Console.WriteLine(" aparecen en esta misma pantalla, en vivo, a medida que ocurren.");
Console.WriteLine("==================================================================");

// Se deja explícito qué entorno quedó activo (y por lo tanto qué appsettings.<Entorno>.json se
// mezcló, si existe): la fuente más común de un "WorkingDirectory mal armado" es correr en un
// entorno distinto al que se editó -por ejemplo, sin DOTNET_ENVIRONMENT=Development no se aplica
// appsettings.Development.json, y el trabajo termina usando la ruta pensada para producción-.
host.Services.GetRequiredService<ILogger<Program>>()
    .LogInformation("Entorno activo (DOTNET_ENVIRONMENT): {Environment}", builder.Environment.EnvironmentName);

await host.RunAsync();
