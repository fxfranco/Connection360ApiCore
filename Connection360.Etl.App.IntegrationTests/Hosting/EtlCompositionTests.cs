using System.Net;
using Connection360.Etl.App.Extensions;
using Connection360.Etl.App.IntegrationTests.Infrastructure;
using Connection360.Etl.Application.Ports;
using Connection360.Etl.Application.UseCases;
using Connection360.Etl.Domain.Constants;
using Connection360.Etl.Domain.Enums;
using Connection360.Etl.Domain.Ports.Persistence;
using Connection360.Etl.Infrastructure.Configuration;
using Connection360.Etl.Infrastructure.DependencyInjection;
using Connection360.Etl.Infrastructure.ExternalApi;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Connection360.Etl.App.IntegrationTests.Hosting
{
    /// <summary>
    /// Integración de la raíz de composición REAL del ETL: appsettings.json real +
    /// AddEtlApplicationServices + AddEtlInfrastructure (servicios de dominio, gateway, HttpClients
    /// nombrados, opciones). Solo se sustituyen los bordes: la persistencia (IUnitOfWork en memoria)
    /// y el HttpMessageHandler primario (StubApiHandler). Los casos de uso corren de verdad.
    /// </summary>
    public class EtlCompositionTests
    {
        private static readonly String[] OperationalFields =
        {
            ExternalDataFields.DocumentNumber, ExternalDataFields.State, ExternalDataFields.Comment,
            ExternalDataFields.CommentDate, ExternalDataFields.ClientNit, ExternalDataFields.Origin,
        };

        private static readonly String[] LogFields =
        {
            ExternalDataFields.IdLog, ExternalDataFields.DocumentNumber, ExternalDataFields.ChangeDateLog,
            ExternalDataFields.ChangeUserLog, ExternalDataFields.MessageLog, ExternalDataFields.OldStateLog,
            ExternalDataFields.NewStateLog,
        };

        private static Dictionary<String, String> Row(String hbl, String state = "", String comment = "", String commentDate = "", String nit = "", String origin = "") => new()
        {
            [ExternalDataFields.DocumentNumber] = hbl,
            [ExternalDataFields.State] = state,
            [ExternalDataFields.Comment] = comment,
            [ExternalDataFields.CommentDate] = commentDate,
            [ExternalDataFields.ClientNit] = nit,
            [ExternalDataFields.Origin] = origin,
        };

        private static String EmptyPage() => StubApiHandler.Page(OperationalFields);

        /// <summary>Registra respuestas por defecto: BPMS con los datos dados y el resto de APIs vacías.</summary>
        private static StubApiHandler CreateHandler(Func<RecordedRequest, String> bpms, Func<RecordedRequest, String>? sim = null, Func<RecordedRequest, String>? datalogs = null)
        {
            var handler = new StubApiHandler();
            handler.Routes["bpms.test"] = r => StubApiHandler.Ok(bpms(r));
            handler.Routes["sim.test"] = r => StubApiHandler.Ok((sim ?? (_ => EmptyPage()))(r));
            handler.Routes["opencomex.test"] = _ => StubApiHandler.Ok(EmptyPage());
            handler.Routes["asiscomex.test"] = _ => StubApiHandler.Ok(EmptyPage());
            handler.Routes["systemcarrier.test"] = _ => StubApiHandler.Ok(EmptyPage());
            handler.Routes["datalogs.test"] = r => StubApiHandler.Ok((datalogs ?? (_ => StubApiHandler.Page(LogFields)))(r));
            return handler;
        }

        private static ServiceProvider BuildProvider(
            InMemoryEtlStore store,
            StubApiHandler handler,
            IEnumerable<KeyValuePair<String, String?>>? overrides = null,
            Boolean useStubHosts = true)
        {
            var configuration = AppSettingsLoader.Load(overrides, useStubHosts);

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddEtlApplicationServices();
            services.AddEtlInfrastructure(configuration);

            // Bordes sustituidos: persistencia en memoria y HTTP simulado.
            services.AddScoped<IUnitOfWork>(_ => new InMemoryUnitOfWork(store));
            services.ConfigureHttpClientDefaults(builder => builder.ConfigurePrimaryHttpMessageHandler(() => handler));

            return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = false });
        }

        // ---------- Configuración real (appsettings.json) ----------

        [Fact]
        public void AppsettingsReal_SeBindeaALasOpcionesDeInfraestructura()
        {
            using var provider = BuildProvider(new InMemoryEtlStore(), new StubApiHandler(), useStubHosts: false);

            var api = provider.GetRequiredService<IOptions<ExternalApiSettings>>().Value;
            var jobControl = provider.GetRequiredService<IOptions<EtlJobControlSettings>>().Value;
            var tracking = provider.GetRequiredService<IOptions<EtlChangeTrackingSettings>>().Value;

            api.PaginationEnabled.Should().BeFalse();
            api.PageSize.Should().Be(500);
            api.Apis.Keys.Should().BeEquivalentTo("BPMS", "DATALOGS", "SIM", "OPENCOMEX", "ASISCOMEX", "SYSTEMCARRIER");
            api.Apis["BPMS"].DataEndpoint.Should().Be("/api/excelfields/latest-data");
            api.Apis["DATALOGS"].DataEndpoint.Should().Be("/api/excelfields/latestLogs-data");
            api.Apis["SIM"].BaseUrl.Should().Be("https://localhost:44314");
            api.Apis["BPMS"].PageNumberParam.Should().Be("pageNumber");
            api.Apis["BPMS"].PageSizeParam.Should().Be("pageSize");
            api.Apis.Values.Should().OnlyContain(a => a.TimeoutSeconds == 30);
            jobControl.ApplicationDataSheetRetentionDays.Should().Be(30);
            tracking.SystemUser.Should().Be("ETL_CONNECTION360");
        }

        [Fact]
        public void AppsettingsReal_RegistraUnHttpClientNombradoPorCadaApiConBaseYTimeout()
        {
            using var provider = BuildProvider(new InMemoryEtlStore(), new StubApiHandler(), useStubHosts: false);
            var factory = provider.GetRequiredService<IHttpClientFactory>();

            var bpms = factory.CreateClient("BPMS");
            var asiscomex = factory.CreateClient("ASISCOMEX");

            bpms.BaseAddress.Should().Be(new Uri("https://localhost:44313"));
            asiscomex.BaseAddress.Should().Be(new Uri("https://localhost:44316"));
            bpms.Timeout.Should().Be(TimeSpan.FromSeconds(30));
            bpms.DefaultRequestHeaders.Contains("X-Api-Key").Should().BeFalse("el ApiKey versionado está vacío");
        }

        [Fact]
        public void Configuracion_ApiKeyConfigurada_SeEnviaComoCabeceraXApiKey()
        {
            var overrides = new Dictionary<String, String?> { ["ExternalApi:Apis:SIM:ApiKey"] = "clave-sim", ["ExternalApi:Apis:SIM:TimeoutSeconds"] = "7" };
            using var provider = BuildProvider(new InMemoryEtlStore(), new StubApiHandler(), overrides, useStubHosts: false);

            var sim = provider.GetRequiredService<IHttpClientFactory>().CreateClient("SIM");

            sim.DefaultRequestHeaders.GetValues("X-Api-Key").Should().ContainSingle().Which.Should().Be("clave-sim");
            sim.Timeout.Should().Be(TimeSpan.FromSeconds(7));
        }

        [Fact]
        public async Task Composicion_ResuelveLosCasosDeUsoYSusDependenciasDeDominioEInfraestructura()
        {
            using var provider = BuildProvider(new InMemoryEtlStore(), new StubApiHandler());
            await using var scope = provider.CreateAsyncScope();

            scope.ServiceProvider.GetRequiredService<IRunEtlProcessUseCase>().Should().BeOfType<RunEtlProcessUseCase>();
            scope.ServiceProvider.GetRequiredService<IRunLogsEtlProcessUseCase>().Should().BeOfType<RunLogsEtlProcessUseCase>();
            scope.ServiceProvider.GetRequiredService<IPurgeEtlJobControlUseCase>().Should().BeOfType<PurgeEtlJobControlUseCase>();
            scope.ServiceProvider.GetRequiredService<IExternalDataGateway>().Should().BeOfType<ExternalDataApiGateway>();
        }

        // ---------- ETL principal de punta a punta ----------

        [Fact]
        public async Task EtlPrincipal_PrimeraCorrida_EsMigracionInicialYCargaSinDetectarCambios()
        {
            var store = new InMemoryEtlStore();
            var handler = CreateHandler(
                bpms: _ => StubApiHandler.Page(OperationalFields, Row("HBL-001", "CREADO", nit: "900123"), Row("HBL-002", "EN TRANSITO", "c1", "01/10/2026", "900456")),
                sim: _ => StubApiHandler.Page(OperationalFields, Row("HBL-001", origin: "Shanghai")));
            using var provider = BuildProvider(store, handler);
            await using var scope = provider.CreateAsyncScope();
            var useCase = scope.ServiceProvider.GetRequiredService<IRunEtlProcessUseCase>();

            var result = await useCase.ExecuteAsync();

            result.Success.Should().BeTrue(result.ErrorMessage);
            result.IsInitialMigrationRun.Should().BeTrue();
            result.ExtractedRecordsByApi["BPMS"].Should().Be(2);
            result.ExtractedRecordsByApi["SIM"].Should().Be(1);
            result.TransformedRecords.Should().Be(2);
            result.LoadedRecords.Should().Be(2);
            result.StateChangesDetected.Should().Be(0);
            result.CommentChangesDetected.Should().Be(0);

            store.Sheets.Keys.Should().BeEquivalentTo("HBL-001", "HBL-002");
            store.Sheets["HBL-001"].Estado.Should().Be("CREADO");
            store.Sheets["HBL-001"].Origen.Should().Be("Shanghai", "el merge combina las APIs por documento de transporte");
            store.Sheets["HBL-001"].NitCliente.Should().Be("900123");
            store.Sheets["HBL-002"].Comentario.Should().Be("c1");
            store.Sheets["HBL-002"].FechaComentario.Should().Be(new DateTime(2026, 10, 1));
            store.LogRows.Should().BeEmpty();
            store.Outbox.Should().BeEmpty();

            store.JobsOf(EtlJobName.ApplicationDataSheet).Should().ContainSingle().Which.Status.Should().Be("COMPLETED");
            store.JobsOf(EtlJobName.ApplicationDataSheetMigration).Should().ContainSingle().Which.Status.Should().Be("COMPLETED");
            store.JobsOf(EtlJobName.ApplicationDataSheet).Single().PageSize.Should().BeNull("la paginación está deshabilitada en el appsettings");
            store.Transactions.Should().Equal("begin", "commit");
        }

        [Fact]
        public async Task EtlPrincipal_SinPaginacion_ConsultaCadaApiOperativaUnaVezSinParametrosDePagina()
        {
            var store = new InMemoryEtlStore();
            var handler = CreateHandler(bpms: _ => StubApiHandler.Page(OperationalFields, Row("HBL-001", "CREADO")));
            using var provider = BuildProvider(store, handler);
            await using var scope = provider.CreateAsyncScope();

            await scope.ServiceProvider.GetRequiredService<IRunEtlProcessUseCase>().ExecuteAsync();

            handler.Requests.Select(r => r.Host).Should().BeEquivalentTo("bpms.test", "sim.test", "opencomex.test", "asiscomex.test", "systemcarrier.test");
            handler.Requests.Should().OnlyContain(r => r.Path == "/api/excelfields/latest-data" && r.Query.Count == 0);
        }

        [Fact]
        public async Task EtlPrincipal_SegundaCorrida_DetectaCambiosDeEstadoYComentarioYUsaElUsuarioDeSistemaConfigurado()
        {
            var store = new InMemoryEtlStore();
            Int32 run = 1;
            var handler = CreateHandler(bpms: _ => run == 1
                ? StubApiHandler.Page(OperationalFields,
                    Row("HBL-001", "CREADO", nit: "900123"),
                    Row("HBL-002", "EN TRANSITO", "c1", "01/10/2026", "900456"))
                : StubApiHandler.Page(OperationalFields,
                    Row("HBL-001", "EN TRANSITO", nit: "900123"),                    // cambio de estado
                    Row("HBL-002", "EN TRANSITO", "c2", "01/10/2026", "900456"),     // cambio de comentario
                    Row("HBL-003", "CREADO", nit: "900789")));                       // documento nuevo
            var overrides = new Dictionary<String, String?> { ["EtlChangeTracking:SystemUser"] = "BOT_PRUEBAS" };
            using var provider = BuildProvider(store, handler, overrides);

            await using (var first = provider.CreateAsyncScope())
            {
                (await first.ServiceProvider.GetRequiredService<IRunEtlProcessUseCase>().ExecuteAsync()).Success.Should().BeTrue();
            }

            run = 2;
            await using var second = provider.CreateAsyncScope();
            var result = await second.ServiceProvider.GetRequiredService<IRunEtlProcessUseCase>().ExecuteAsync();

            result.Success.Should().BeTrue(result.ErrorMessage);
            result.IsInitialMigrationRun.Should().BeFalse("la migración inicial ya quedó COMPLETED");
            result.StateChangesDetected.Should().Be(2);
            result.CommentChangesDetected.Should().Be(1);

            // log_status_tracking: una fila por cambio de estado, con el usuario de sistema configurado y el Id real de la operación.
            store.LogRows.Should().HaveCount(2);
            store.LogRows.Should().OnlyContain(l => l.UsuarioCambio == "BOT_PRUEBAS");
            var changed = store.LogRows.Single(l => l.DocumentoTransporteHbl == "HBL-001");
            changed.EstadoAnterior.Should().Be("CREADO");
            changed.NuevoEstado.Should().Be("EN TRANSITO");
            changed.IdOperacion.Should().Be(store.SheetIds["HBL-001"]);
            var created = store.LogRows.Single(l => l.DocumentoTransporteHbl == "HBL-003");
            created.EstadoAnterior.Should().BeEmpty();
            created.IdOperacion.Should().Be(store.SheetIds["HBL-003"], "el Id del documento nuevo se resuelve después del upsert");

            // outbox: 2 ChangeState + 1 Comment.
            store.Outbox.Should().HaveCount(3);
            store.Outbox.Count(o => o.EventType == EtlChangeEventType.ChangeState.ToDbValue()).Should().Be(2);
            store.Outbox.Count(o => o.EventType == EtlChangeEventType.Comment.ToDbValue()).Should().Be(1);
            store.Outbox.Should().OnlyContain(o => o.Payload.Contains("HBL-00"));

            store.Sheets["HBL-001"].Estado.Should().Be("EN TRANSITO");
            store.Sheets["HBL-002"].Comentario.Should().Be("c2");
            store.JobsOf(EtlJobName.ApplicationDataSheet).Should().HaveCount(2).And.OnlyContain(j => j.Status == "COMPLETED");
            store.JobsOf(EtlJobName.ApplicationDataSheetMigration).Should().ContainSingle("solo la primera corrida registra la migración");
        }

        [Fact]
        public async Task EtlPrincipal_ConPaginacionHabilitada_PideCadaPaginaConLosParametrosConfigurados()
        {
            var store = new InMemoryEtlStore();
            var handler = CreateHandler(bpms: r => r.Query["pageNumber"] switch
            {
                "1" => StubApiHandler.Page(OperationalFields, Row("HBL-001", "A"), Row("HBL-002", "B")),
                "2" => StubApiHandler.Page(OperationalFields, Row("HBL-003", "C")),
                _ => EmptyPage(),
            });
            var overrides = new Dictionary<String, String?>
            {
                ["ExternalApi:PaginationEnabled"] = "true",
                ["ExternalApi:PageSize"] = "2",
            };
            using var provider = BuildProvider(store, handler, overrides);
            await using var scope = provider.CreateAsyncScope();

            var result = await scope.ServiceProvider.GetRequiredService<IRunEtlProcessUseCase>().ExecuteAsync();

            result.Success.Should().BeTrue(result.ErrorMessage);
            result.LoadedRecords.Should().Be(3);
            store.Sheets.Keys.Should().BeEquivalentTo("HBL-001", "HBL-002", "HBL-003");

            var bpmsRequests = handler.RequestsTo("bpms.test").ToList();
            bpmsRequests.Select(r => r.Query["pageNumber"]).Should().Equal("1", "2");
            bpmsRequests.Should().OnlyContain(r => r.Query["pageSize"] == "2");

            var job = store.JobsOf(EtlJobName.ApplicationDataSheet).Single();
            job.PageSize.Should().Be(2, "el mismo PageSize global se registra en etl_job_control");
            job.LastProcessedPage.Should().Be(2);
            job.TotalRecordsProcessed.Should().Be(3);
            job.Status.Should().Be("COMPLETED");
            store.Transactions.Should().Equal("begin", "commit", "begin", "commit");
        }

        [Theory]
        [InlineData("true", "0")]
        [InlineData("false", "2")]
        public async Task EtlPrincipal_PaginacionDeshabilitadaOPageSizeInvalido_NoPaginaYNoRegistraPageSize(String enabled, String pageSize)
        {
            var store = new InMemoryEtlStore();
            var handler = CreateHandler(bpms: _ => StubApiHandler.Page(OperationalFields, Row("HBL-001", "A")));
            var overrides = new Dictionary<String, String?> { ["ExternalApi:PaginationEnabled"] = enabled, ["ExternalApi:PageSize"] = pageSize };
            using var provider = BuildProvider(store, handler, overrides);
            await using var scope = provider.CreateAsyncScope();

            var result = await scope.ServiceProvider.GetRequiredService<IRunEtlProcessUseCase>().ExecuteAsync();

            result.Success.Should().BeTrue(result.ErrorMessage);
            handler.Requests.Should().OnlyContain(r => r.Query.Count == 0);
            store.JobsOf(EtlJobName.ApplicationDataSheet).Single().PageSize.Should().BeNull();
        }

        [Fact]
        public async Task EtlPrincipal_ApiExternaResponde500_FallaYMarcaLosJobsComoFailed()
        {
            var store = new InMemoryEtlStore();
            var handler = CreateHandler(bpms: _ => EmptyPage());
            handler.Routes["bpms.test"] = _ => (HttpStatusCode.InternalServerError, "{\"error\":\"boom\"}");
            using var provider = BuildProvider(store, handler);
            await using var scope = provider.CreateAsyncScope();

            var result = await scope.ServiceProvider.GetRequiredService<IRunEtlProcessUseCase>().ExecuteAsync();

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("500");
            store.Sheets.Should().BeEmpty();
            store.JobsOf(EtlJobName.ApplicationDataSheet).Should().ContainSingle().Which.Status.Should().Be("FAILED");
            store.JobsOf(EtlJobName.ApplicationDataSheetMigration).Should().ContainSingle().Which.Status.Should().Be("FAILED");
        }

        [Fact]
        public async Task EtlPrincipal_FallaLaBaseDeDatosEnUnaRonda_HaceRollbackYMarcaFailed()
        {
            var store = new InMemoryEtlStore { FailOnUpsert = true };
            var handler = CreateHandler(bpms: _ => StubApiHandler.Page(OperationalFields, Row("HBL-001", "A")));
            using var provider = BuildProvider(store, handler);
            await using var scope = provider.CreateAsyncScope();

            var result = await scope.ServiceProvider.GetRequiredService<IRunEtlProcessUseCase>().ExecuteAsync();

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Fallo simulado");
            store.Transactions.Should().Equal("begin", "rollback");
            store.Sheets.Should().BeEmpty();
            store.JobsOf(EtlJobName.ApplicationDataSheet).Single().Status.Should().Be("FAILED");
        }

        [Fact]
        public async Task EtlPrincipal_CancelacionSolicitada_TerminaSinExitoYMarcaFailed()
        {
            var store = new InMemoryEtlStore();
            var handler = CreateHandler(bpms: _ => StubApiHandler.Page(OperationalFields, Row("HBL-001", "A")));
            using var provider = BuildProvider(store, handler);
            await using var scope = provider.CreateAsyncScope();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var result = await scope.ServiceProvider.GetRequiredService<IRunEtlProcessUseCase>().ExecuteAsync(cts.Token);

            result.Success.Should().BeFalse();
            store.Sheets.Should().BeEmpty();
            store.JobsOf(EtlJobName.ApplicationDataSheet).Single().Status.Should().Be("FAILED");
        }

        // ---------- ETL de logs de punta a punta ----------

        private static Dictionary<String, String> LogRow(String id, String hbl, String oldState, String newState) => new()
        {
            [ExternalDataFields.IdLog] = id,
            [ExternalDataFields.DocumentNumber] = hbl,
            [ExternalDataFields.ChangeDateLog] = "02/10/2026 10:30:00",
            [ExternalDataFields.ChangeUserLog] = "operador",
            [ExternalDataFields.MessageLog] = "cambio",
            [ExternalDataFields.OldStateLog] = oldState,
            [ExternalDataFields.NewStateLog] = newState,
        };

        [Fact]
        public async Task EtlLogs_PrimeraCorrida_CargaElHistoricoYSeEjecutaUnaSolaVez()
        {
            var store = new InMemoryEtlStore();
            var handler = CreateHandler(
                bpms: _ => EmptyPage(),
                datalogs: _ => StubApiHandler.Page(LogFields, LogRow("1", "HBL-001", "CREADO", "EN TRANSITO"), LogRow("2", "HBL-002", "", "CREADO"), LogRow("3", "", "", "X")));
            using var provider = BuildProvider(store, handler);

            await using (var scope = provider.CreateAsyncScope())
            {
                var result = await scope.ServiceProvider.GetRequiredService<IRunLogsEtlProcessUseCase>().ExecuteAsync();

                result.Success.Should().BeTrue(result.ErrorMessage);
                result.ExtractedRecordsByApi["DATALOGS"].Should().Be(3);
                result.TransformedRecords.Should().Be(2, "la fila sin documento se descarta");
                result.LoadedRecords.Should().Be(2);
            }

            store.LogRows.Select(l => l.DocumentoTransporteHbl).Should().BeEquivalentTo("HBL-001", "HBL-002");
            store.LogRows.Single(l => l.DocumentoTransporteHbl == "HBL-001").NuevoEstado.Should().Be("EN TRANSITO");
            store.LogRows.Should().OnlyContain(l => l.UsuarioCambio == "operador");
            store.JobsOf(EtlJobName.LogStatusTracking).Should().ContainSingle().Which.Status.Should().Be("COMPLETED");
            handler.RequestsTo("datalogs.test").Should().ContainSingle().Which.Path.Should().Be("/api/excelfields/latestLogs-data");

            // Segunda corrida: ya hay un COMPLETED -> no vuelve a consultar ni a cargar.
            Int32 requestsBefore = handler.Requests.Count;
            await using var again = provider.CreateAsyncScope();
            var second = await again.ServiceProvider.GetRequiredService<IRunLogsEtlProcessUseCase>().ExecuteAsync();

            second.Success.Should().BeTrue();
            second.LoadedRecords.Should().Be(0);
            handler.Requests.Count.Should().Be(requestsBefore);
            store.LogRows.Should().HaveCount(2);
        }

        [Fact]
        public async Task EtlLogs_ApiDeLogsResponde500_FallaYMarcaElJobComoFailed()
        {
            var store = new InMemoryEtlStore();
            var handler = CreateHandler(bpms: _ => EmptyPage());
            handler.Routes["datalogs.test"] = _ => (HttpStatusCode.BadGateway, "no disponible");
            using var provider = BuildProvider(store, handler);
            await using var scope = provider.CreateAsyncScope();

            var result = await scope.ServiceProvider.GetRequiredService<IRunLogsEtlProcessUseCase>().ExecuteAsync();

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("502");
            store.JobsOf(EtlJobName.LogStatusTracking).Should().ContainSingle().Which.Status.Should().Be("FAILED");
        }

        [Fact]
        public async Task EtlLogs_ConPaginacion_ConsultaDatalogsPaginaPorPagina()
        {
            var store = new InMemoryEtlStore();
            var handler = CreateHandler(
                bpms: _ => EmptyPage(),
                datalogs: r => r.Query["pageNumber"] switch
                {
                    "1" => StubApiHandler.Page(LogFields, LogRow("1", "HBL-001", "", "A"), LogRow("2", "HBL-002", "", "B")),
                    "2" => StubApiHandler.Page(LogFields, LogRow("3", "HBL-003", "", "C")),
                    _ => StubApiHandler.Page(LogFields),
                });
            var overrides = new Dictionary<String, String?> { ["ExternalApi:PaginationEnabled"] = "true", ["ExternalApi:PageSize"] = "2" };
            using var provider = BuildProvider(store, handler, overrides);
            await using var scope = provider.CreateAsyncScope();

            var result = await scope.ServiceProvider.GetRequiredService<IRunLogsEtlProcessUseCase>().ExecuteAsync();

            result.Success.Should().BeTrue(result.ErrorMessage);
            result.LoadedRecords.Should().Be(3);
            handler.RequestsTo("datalogs.test").Select(r => r.Query["pageNumber"]).Should().Equal("1", "2");
            var job = store.JobsOf(EtlJobName.LogStatusTracking).Single();
            job.PageSize.Should().Be(2);
            job.LastProcessedPage.Should().Be(2);
        }

        // ---------- Depuración de etl_job_control ----------

        [Fact]
        public async Task Depuracion_ConRetencionDeCeroDias_EliminaLosJobsDeApplicationDataSheetPeroNoLosDeLogs()
        {
            var store = new InMemoryEtlStore();
            var handler = CreateHandler(bpms: _ => StubApiHandler.Page(OperationalFields, Row("HBL-001", "A")));
            var overrides = new Dictionary<String, String?> { ["EtlJobControl:ApplicationDataSheetRetentionDays"] = "0" };
            using var provider = BuildProvider(store, handler, overrides);

            await using (var scope = provider.CreateAsyncScope())
            {
                await scope.ServiceProvider.GetRequiredService<IRunLogsEtlProcessUseCase>().ExecuteAsync();
                await scope.ServiceProvider.GetRequiredService<IRunEtlProcessUseCase>().ExecuteAsync();
            }

            await Task.Delay(20); // asegura que updated_at quede estrictamente anterior al instante de la depuración
            await using var purgeScope = provider.CreateAsyncScope();
            Int32 deleted = await purgeScope.ServiceProvider.GetRequiredService<IPurgeEtlJobControlUseCase>().ExecuteAsync();

            deleted.Should().Be(1);
            store.JobsOf(EtlJobName.ApplicationDataSheet).Should().BeEmpty();
            store.JobsOf(EtlJobName.LogStatusTracking).Should().ContainSingle("los registros de log_status_tracking nunca se depuran");
            store.JobsOf(EtlJobName.ApplicationDataSheetMigration).Should().ContainSingle();
        }

        [Fact]
        public async Task Depuracion_ConRetencionDeLaConfiguracionReal_NoEliminaRegistrosRecientes()
        {
            var store = new InMemoryEtlStore();
            var handler = CreateHandler(bpms: _ => StubApiHandler.Page(OperationalFields, Row("HBL-001", "A")));
            using var provider = BuildProvider(store, handler);

            await using var scope = provider.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IRunEtlProcessUseCase>().ExecuteAsync();
            Int32 deleted = await scope.ServiceProvider.GetRequiredService<IPurgeEtlJobControlUseCase>().ExecuteAsync();

            deleted.Should().Be(0);
            store.JobsOf(EtlJobName.ApplicationDataSheet).Should().ContainSingle();
        }
    }
}
