using Connection360.Etl.App.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Connection360.Etl.App.IntegrationTests.Hosting
{
    /// <summary>
    /// Ejecuta el Program.cs REAL de Connection360.Etl.App (host real, configuración real, DI real:
    /// AddEtlApplicationServices + AddEtlInfrastructure + NpgsqlDataSource) sustituyendo únicamente
    /// los tres casos de uso por fakes, de modo que no haya BD, Kafka ni HTTP.
    /// </summary>
    public class ProgramTests
    {
        private const String ValidConnection = "Host=db.connection360.test;Port=5432;Database=etl_test;Username=etl_user;Password=no-se-usa";

        private static ProgramRun RunProgram(ProgramFakes fakes, String connectionString = ValidConnection, params String[] extraArgs)
        {
            var args = new List<String> { $"--ConnectionStrings:PostgresConnection={connectionString}" };
            args.AddRange(extraArgs);
            return EtlProgramRunner.Run(fakes.Register, args.ToArray());
        }

        // ---------- Flujo completo ----------

        [Fact]
        public void Program_TodoExitoso_EjecutaLogsLuegoPrincipalLuegoDepuracionYSaleConCero()
        {
            var fakes = new ProgramFakes();

            var run = RunProgram(fakes);

            run.Exception.Should().BeNull();
            run.ExitCode.Should().Be(0);
            fakes.Probe.Calls.Should().Equal("logs", "main", "purge");
        }

        [Fact]
        public void Program_CadaPasoCorreEnSuPropioScopeYLoLibera()
        {
            var fakes = new ProgramFakes();

            RunProgram(fakes);

            fakes.Probe.ScopeIds.Should().HaveCount(3).And.OnlyHaveUniqueItems();
            fakes.Probe.ScopesDisposed.Should().Be(3);
        }

        [Fact]
        public void Program_PasaUnTokenDeCancelacionNoCanceladoACadaCasoDeUso()
        {
            var fakes = new ProgramFakes();

            RunProgram(fakes);

            fakes.Probe.TokenWasCancellable.Should().Equal(true, true, true);
            fakes.Probe.TokenWasCancelled.Should().Equal(false, false, false);
        }

        [Fact]
        public void Program_RegistraUnNpgsqlDataSourceSingletonConLaCadenaConfigurada()
        {
            var fakes = new ProgramFakes();

            RunProgram(fakes);

            fakes.Probe.DataSources.Should().HaveCount(3);
            fakes.Probe.DataSources.Distinct().Should().ContainSingle("el NpgsqlDataSource debe ser singleton para todo el proceso");
            var connectionString = fakes.Probe.DataSources[0].ConnectionString;
            connectionString.Should().Contain("db.connection360.test").And.Contain("etl_test");
            connectionString.Should().NotContain("no-se-usa", "Npgsql no expone la contraseña en ConnectionString");
        }

        [Fact]
        public void Program_RegistraLosResultadosDeCadaProcesoEnElLog()
        {
            var fakes = new ProgramFakes();

            var run = RunProgram(fakes);

            var messages = run.Logs.Entries.Where(e => e.Category == "Program").Select(e => e.Message).ToList();
            messages.Should().Contain("Extract [DATALOGS]: 3 registros");
            messages.Should().Contain("Extract [BPMS]: 5 registros");
            messages.Should().Contain("Extract [SIM]: 2 registros");
            messages.Should().Contain(m => m.StartsWith("Corrida ETL (logs) finalizada. Éxito=True. Transformados=3. Cargados=3. Duración=00:00:05"));
            messages.Should().Contain(m => m.StartsWith("Corrida ETL (bodega de datos) finalizada. Éxito=True. Transformados=5. Cargados=4. Duración=00:00:05"));
            messages.Should().Contain("Depuración de etl_job_control finalizada: 4 registro(s) eliminados.");
            run.Logs.Entries.Should().NotContain(e => e.Level == LogLevel.Error);
        }

        // ---------- Códigos de salida ----------

        [Fact]
        public void Program_FallaElProcesoDeLogs_SaleConUnoYAunAsiCorreElPrincipalYLaDepuracion()
        {
            var fakes = new ProgramFakes { LogsResult = () => ProgramFakes.FailureResult("falló-logs") };

            var run = RunProgram(fakes);

            run.ExitCode.Should().Be(1);
            fakes.Probe.Calls.Should().Equal("logs", "main", "purge");
            run.Logs.Entries.Should().Contain(e => e.Level == LogLevel.Error && e.Message == "Detalle del error: falló-logs");
            run.Logs.Entries.Should().Contain(e => e.Message.StartsWith("Corrida ETL (logs) finalizada. Éxito=False"));
        }

        [Fact]
        public void Program_FallaElProcesoPrincipal_SaleConUnoYAunAsiCorreLaDepuracion()
        {
            var fakes = new ProgramFakes { MainResult = () => ProgramFakes.FailureResult("falló-principal") };

            var run = RunProgram(fakes);

            run.ExitCode.Should().Be(1);
            fakes.Probe.Calls.Should().Equal("logs", "main", "purge");
            run.Logs.Entries.Should().Contain(e => e.Level == LogLevel.Error && e.Message == "Detalle del error: falló-principal");
            run.Logs.Entries.Should().Contain(e => e.Message.StartsWith("Corrida ETL (bodega de datos) finalizada. Éxito=False"));
        }

        [Fact]
        public void Program_FallanAmbosProcesos_SaleConUnoYRegistraAmbosErrores()
        {
            var fakes = new ProgramFakes
            {
                LogsResult = () => ProgramFakes.FailureResult("e1"),
                MainResult = () => ProgramFakes.FailureResult("e2"),
            };

            var run = RunProgram(fakes);

            run.ExitCode.Should().Be(1);
            run.Logs.Entries.Where(e => e.Level == LogLevel.Error).Select(e => e.Message)
                .Should().BeEquivalentTo("Detalle del error: e1", "Detalle del error: e2");
        }

        [Fact]
        public void Program_LaDepuracionNoAfectaElCodigoDeSalida()
        {
            var fakes = new ProgramFakes { PurgeResult = () => 0 };

            var run = RunProgram(fakes);

            run.ExitCode.Should().Be(0);
            run.Logs.Entries.Should().Contain(e => e.Message == "Depuración de etl_job_control finalizada: 0 registro(s) eliminados.");
        }

        [Fact]
        public void Program_ProcesoSinRegistrosExtraidos_NoEscribeLineasExtract()
        {
            var fakes = new ProgramFakes
            {
                LogsResult = () => ProgramFakes.SuccessResult(0, 0),
                MainResult = () => ProgramFakes.SuccessResult(0, 0),
            };

            var run = RunProgram(fakes);

            run.ExitCode.Should().Be(0);
            run.Logs.Entries.Should().NotContain(e => e.Message.StartsWith("Extract ["));
        }

        // ---------- Excepciones no controladas ----------

        [Fact]
        public void Program_UnCasoDeUsoLanzaExcepcion_SePropagaYNoSeEjecutanLosPasosSiguientes()
        {
            var fakes = new ProgramFakes { LogsResult = () => throw new InvalidOperationException("catástrofe") };

            var run = RunProgram(fakes);

            run.Exception.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Be("catástrofe");
            fakes.Probe.Calls.Should().Equal("logs");
            fakes.Probe.ScopesDisposed.Should().Be(1, "el scope se libera aunque el caso de uso falle");
        }

        // ---------- Cadena de conexión ----------

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Program_CadenaDeConexionVaciaOEnBlanco_LanzaInvalidOperationExceptionClaraSinEjecutarNada(String connectionString)
        {
            var fakes = new ProgramFakes();

            var run = RunProgram(fakes, connectionString);

            run.Exception.Should().BeOfType<InvalidOperationException>()
                .Which.Message.Should().Be("La conexión 'PostgresConnection' no está configurada en appsettings.json.");
            fakes.Probe.Calls.Should().BeEmpty();
        }

        [Fact]
        public void Program_SinCadenaDeConexion_UsaElAppsettingsBaseConValorEnBlancoYFalla()
        {
            // El appsettings.json versionado deja PostgresConnection en "" a propósito (no se versionan credenciales).
            var fakes = new ProgramFakes();

            var run = EtlProgramRunner.Run(fakes.Register);

            run.Exception.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Contain("PostgresConnection");
            fakes.Probe.Calls.Should().BeEmpty();
        }

        // ---------- Configuración enlazada dentro del host real ----------

        [Fact]
        public void Program_EnlazaLaConfiguracionExternalApiEnElHostReal()
        {
            ExternalApiSnapshot? snapshot = null;
            var fakes = new ProgramFakes();

            var run = EtlProgramRunner.Run(
                services =>
                {
                    fakes.Register(services);
                    services.AddSingleton<Action<IServiceProvider>>(sp => snapshot = ExternalApiSnapshot.From(sp));
                    services.AddScoped<Connection360.Etl.Application.Ports.IPurgeEtlJobControlUseCase>(sp =>
                    {
                        sp.GetRequiredService<Action<IServiceProvider>>()(sp);
                        return new PurgeProbe();
                    });
                },
                $"--ConnectionStrings:PostgresConnection={ValidConnection}",
                "--ExternalApi:PaginationEnabled=true",
                "--ExternalApi:PageSize=123",
                "--ExternalApi:Apis:SIM:ApiKey=clave-sim");

            run.Exception.Should().BeNull();
            snapshot.Should().NotBeNull();
            snapshot!.PaginationEnabled.Should().BeTrue();
            snapshot.PageSize.Should().Be(123);
            snapshot.ApiNames.Should().BeEquivalentTo("BPMS", "DATALOGS", "SIM", "OPENCOMEX", "ASISCOMEX", "SYSTEMCARRIER");
            snapshot.SimApiKey.Should().Be("clave-sim");
            snapshot.RetentionDays.Should().Be(30);
            snapshot.SystemUser.Should().Be("ETL_CONNECTION360");
            snapshot.BpmsBaseAddress.Should().Be("https://localhost:44313/");
            snapshot.DataSourceIsSingleton.Should().BeTrue();
        }

        private sealed class PurgeProbe : Connection360.Etl.Application.Ports.IPurgeEtlJobControlUseCase
        {
            public Task<Int32> ExecuteAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        }

        private sealed record ExternalApiSnapshot(
            Boolean PaginationEnabled,
            Int32 PageSize,
            IReadOnlyCollection<String> ApiNames,
            String? SimApiKey,
            Int32 RetentionDays,
            String SystemUser,
            String? BpmsBaseAddress,
            Boolean DataSourceIsSingleton)
        {
            public static ExternalApiSnapshot From(IServiceProvider sp)
            {
                var api = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Connection360.Etl.Infrastructure.ExternalApi.ExternalApiSettings>>().Value;
                var retention = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Connection360.Etl.Infrastructure.Configuration.EtlJobControlSettings>>().Value;
                var tracking = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Connection360.Etl.Infrastructure.Configuration.EtlChangeTrackingSettings>>().Value;
                var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
                var first = sp.GetRequiredService<Npgsql.NpgsqlDataSource>();
                var second = sp.GetRequiredService<Npgsql.NpgsqlDataSource>();
                return new ExternalApiSnapshot(
                    api.PaginationEnabled,
                    api.PageSize,
                    api.Apis.Keys.ToList(),
                    api.Apis["SIM"].ApiKey,
                    retention.ApplicationDataSheetRetentionDays,
                    tracking.SystemUser,
                    httpFactory.CreateClient("BPMS").BaseAddress?.ToString(),
                    ReferenceEquals(first, second));
            }
        }
    }
}
