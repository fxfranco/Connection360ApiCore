using Connection360.Etl.App.Extensions;
using Connection360.Etl.Application.Ports;
using Connection360.Etl.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

// ---------- Composition root del proceso ETL (Extract, Transform, Load) ----------
// Sigue el mismo patrón de arranque que Connection360.Api/Program.cs: un NpgsqlDataSource
// SINGLETON para todo el proceso (pool de conexiones, sin reabrir sockets) y las capas de
// Application/Infrastructure registradas mediante sus propias extensiones de IServiceCollection.
var builder = Host.CreateApplicationBuilder(args);

// String.IsNullOrWhiteSpace (no solo "?? throw" sobre null): un appsettings.json de producción
// con "PostgresConnection": "" (placeholder en blanco a propósito, para no versionar credenciales
// reales) es una cadena VACÍA, no null, así que "??" nunca disparaba este throw -el proceso seguía
// de largo y terminaba crasheando más adelante, sin manejar, dentro de NpgsqlDataSource.Create(""),
// con un mensaje críptico y un código de salida de excepción no controlada en vez de este mensaje
// claro.
var connectionString = builder.Configuration.GetConnectionString("PostgresConnection");
if (String.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("La conexión 'PostgresConnection' no está configurada en appsettings.json.");

builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));

builder.Services.AddEtlApplicationServices();          // Application (caso de uso orquestador)
builder.Services.AddEtlInfrastructure(builder.Configuration); // Infrastructure (APIs externas + PostgreSQL)

using IHost host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();

// Ctrl+C -> cancelación cooperativa del proceso ETL en curso.
using var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationTokenSource.Cancel();
};

// IUnitOfWork (y por lo tanto DbSession/su conexión de PostgreSQL) es SCOPED, igual que en
// Connection360.Api: se crea un scope por corrida y se libera con CreateAsyncScope/await using,
// ya que UnitOfWork solo implementa IAsyncDisposable (ver la corrección aplicada en
// Connection360.Infrastructure.Messaging.OutboxPublisherWorker por el mismo motivo).

// ---------- 1. Proceso ETL principal: bodega de datos de envíos (connection360write.application_data_sheet) ----------
Boolean mainEtlSuccess;
await using (AsyncServiceScope scope = host.Services.CreateAsyncScope())
{
    var runEtlProcessUseCase = scope.ServiceProvider.GetRequiredService<IRunEtlProcessUseCase>();

    var result = await runEtlProcessUseCase.ExecuteAsync(cancellationTokenSource.Token);

    foreach (var (apiName, count) in result.ExtractedRecordsByApi)
    {
        logger.LogInformation("Extract [{Api}]: {Count} registros", apiName, count);
    }

    logger.LogInformation(
        "Corrida ETL (bodega de datos) finalizada. Éxito={Success}. Transformados={Transformed}. Cargados={Loaded}. Duración={Duration}.",
        result.Success, result.TransformedRecords, result.LoadedRecords, result.Duration);

    if (!result.Success)
    {
        logger.LogError("Detalle del error: {ErrorMessage}", result.ErrorMessage);
    }

    mainEtlSuccess = result.Success;
}

// ---------- 2. Proceso ETL de logs: independiente del anterior (DATALOGS -> connection360write.log_status_tracking) ----------
// Corre después del proceso principal (scope propio, transacción propia): su éxito o fracaso no
// depende del resultado del paso 1 ni lo afecta, ya que son flujos completamente independientes.
Boolean logsEtlSuccess;
await using (AsyncServiceScope scope = host.Services.CreateAsyncScope())
{
    var runLogsEtlProcessUseCase = scope.ServiceProvider.GetRequiredService<IRunLogsEtlProcessUseCase>();

    var result = await runLogsEtlProcessUseCase.ExecuteAsync(cancellationTokenSource.Token);

    foreach (var (apiName, count) in result.ExtractedRecordsByApi)
    {
        logger.LogInformation("Extract [{Api}]: {Count} registros", apiName, count);
    }

    logger.LogInformation(
        "Corrida ETL (logs) finalizada. Éxito={Success}. Transformados={Transformed}. Cargados={Loaded}. Duración={Duration}.",
        result.Success, result.TransformedRecords, result.LoadedRecords, result.Duration);

    if (!result.Success)
    {
        logger.LogError("Detalle del error: {ErrorMessage}", result.ErrorMessage);
    }

    logsEtlSuccess = result.Success;
}

// ---------- 3. Depuración de connection360write.etl_job_control (proceso independiente) ----------
// Corre al finalizar los dos procesos ETL anteriores, en su propio scope. Es un housekeeping
// aparte: solo depura los registros del job "application_data_sheet" (nunca los de
// "log_status_tracking") y una falla acá no debe afectar el resultado ya obtenido arriba -
// PurgeEtlJobControlUseCase nunca lanza, así que no participa en Environment.ExitCode.
await using (AsyncServiceScope scope = host.Services.CreateAsyncScope())
{
    var purgeEtlJobControlUseCase = scope.ServiceProvider.GetRequiredService<IPurgeEtlJobControlUseCase>();
    Int32 deletedRows = await purgeEtlJobControlUseCase.ExecuteAsync(cancellationTokenSource.Token);

    logger.LogInformation("Depuración de etl_job_control finalizada: {Deleted} registro(s) eliminados.", deletedRows);
}

// Código de salida estándar para que un scheduler (Windows Task Scheduler, cron, etc.) pueda
// detectar si alguna de las dos corridas ETL falló.
Environment.ExitCode = (mainEtlSuccess && logsEtlSuccess) ? 0 : 1;
