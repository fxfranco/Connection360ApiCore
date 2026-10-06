using Connection360.Etl.App.Extensions;
using Connection360.Etl.Application.Ports;
using Connection360.Etl.Application.UseCases;
using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Domain.Enums;
using Connection360.Etl.Domain.Interfaces;
using Connection360.Etl.Domain.Ports.Persistence;
using Connection360.Etl.Infrastructure.Configuration;
using Connection360.Etl.Infrastructure.ExternalApi;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Connection360.Etl.App.Test.Extensions
{
    public class ServiceCollectionExtensionsTests
    {
        private readonly Mock<IExternalDataGateway> _gateway = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<IEtlJobControlRepository> _jobControlRepository = new();
        private readonly Mock<IDynamicDataSetMerger> _merger = new();
        private readonly Mock<IShipmentsDataSheetMappingService> _shipmentsMapping = new();
        private readonly Mock<ILogStatusMappingService> _logsMapping = new();
        private readonly Mock<IApplicationDataSheetChangeDetector> _changeDetector = new();
        private readonly Mock<IEtlChangeMessageCatalog> _messageCatalog = new();

        public ServiceCollectionExtensionsTests()
        {
            _unitOfWork.Setup(u => u.GetRepository<IEtlJobControlRepository>()).Returns(_jobControlRepository.Object);
            _jobControlRepository
                .Setup(r => r.HasCompletedRunAsync(It.IsAny<EtlJobName>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _jobControlRepository
                .Setup(r => r.StartRunAsync(It.IsAny<EtlJobName>(), It.IsAny<Int32?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(77L);
            _gateway
                .Setup(g => g.FetchDataPagedAsync(It.IsAny<String>(), It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>()))
                .Returns(() => NoPages());
        }

        private static async IAsyncEnumerable<DynamicDataSet> NoPages()
        {
            await Task.CompletedTask;
            yield break;
        }

        /// <summary>Colección con TODAS las dependencias que las fábricas de AddEtlApplicationServices resuelven.</summary>
        private ServiceCollection CreateServices(
            Boolean paginationEnabled = false,
            Int32 pageSize = 500,
            Int32 retentionDays = 30,
            String systemUser = "ETL_TEST")
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IOptions<ExternalApiSettings>>(Options.Create(new ExternalApiSettings { PaginationEnabled = paginationEnabled, PageSize = pageSize }));
            services.AddSingleton<IOptions<EtlChangeTrackingSettings>>(Options.Create(new EtlChangeTrackingSettings { SystemUser = systemUser }));
            services.AddSingleton<IOptions<EtlJobControlSettings>>(Options.Create(new EtlJobControlSettings { ApplicationDataSheetRetentionDays = retentionDays }));
            services.AddSingleton(_gateway.Object);
            services.AddSingleton(_merger.Object);
            services.AddSingleton(_shipmentsMapping.Object);
            services.AddSingleton(_logsMapping.Object);
            services.AddSingleton(_unitOfWork.Object);
            services.AddSingleton(_changeDetector.Object);
            services.AddSingleton(_messageCatalog.Object);
            return services;
        }

        // ---------- Registro ----------

        [Fact]
        public void AddEtlApplicationServices_Siempre_DevuelveLaMismaColeccionParaEncadenar()
        {
            var services = new ServiceCollection();

            var returned = services.AddEtlApplicationServices();

            returned.Should().BeSameAs(services);
        }

        [Theory]
        [InlineData(typeof(IRunEtlProcessUseCase))]
        [InlineData(typeof(IRunLogsEtlProcessUseCase))]
        [InlineData(typeof(IPurgeEtlJobControlUseCase))]
        public void AddEtlApplicationServices_RegistraCadaCasoDeUsoComoScopedConFabrica(Type serviceType)
        {
            var services = new ServiceCollection();

            services.AddEtlApplicationServices();

            var descriptor = services.Should().ContainSingle(d => d.ServiceType == serviceType).Subject;
            descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
            descriptor.ImplementationFactory.Should().NotBeNull();
        }

        [Fact]
        public void AddEtlApplicationServices_RegistraExactamenteTresServicios()
        {
            var services = new ServiceCollection();

            services.AddEtlApplicationServices();

            services.Should().HaveCount(3);
        }

        // ---------- Resolución ----------

        [Fact]
        public void Resolver_IRunEtlProcessUseCase_DevuelveRunEtlProcessUseCase()
        {
            using var provider = CreateServices().AddEtlApplicationServices().BuildServiceProvider();
            using var scope = provider.CreateScope();

            var useCase = scope.ServiceProvider.GetRequiredService<IRunEtlProcessUseCase>();

            useCase.Should().BeOfType<RunEtlProcessUseCase>();
        }

        [Fact]
        public void Resolver_IRunLogsEtlProcessUseCase_DevuelveRunLogsEtlProcessUseCase()
        {
            using var provider = CreateServices().AddEtlApplicationServices().BuildServiceProvider();
            using var scope = provider.CreateScope();

            var useCase = scope.ServiceProvider.GetRequiredService<IRunLogsEtlProcessUseCase>();

            useCase.Should().BeOfType<RunLogsEtlProcessUseCase>();
        }

        [Fact]
        public void Resolver_IPurgeEtlJobControlUseCase_DevuelvePurgeEtlJobControlUseCase()
        {
            using var provider = CreateServices().AddEtlApplicationServices().BuildServiceProvider();
            using var scope = provider.CreateScope();

            var useCase = scope.ServiceProvider.GetRequiredService<IPurgeEtlJobControlUseCase>();

            useCase.Should().BeOfType<PurgeEtlJobControlUseCase>();
        }

        [Fact]
        public void Resolver_DentroDeUnMismoScope_DevuelveLaMismaInstanciaYEnOtroScopeUnaNueva()
        {
            using var provider = CreateServices().AddEtlApplicationServices().BuildServiceProvider(validateScopes: true);

            using var scopeA = provider.CreateScope();
            var a1 = scopeA.ServiceProvider.GetRequiredService<IRunEtlProcessUseCase>();
            var a2 = scopeA.ServiceProvider.GetRequiredService<IRunEtlProcessUseCase>();

            using var scopeB = provider.CreateScope();
            var b1 = scopeB.ServiceProvider.GetRequiredService<IRunEtlProcessUseCase>();

            a2.Should().BeSameAs(a1);
            b1.Should().NotBeSameAs(a1);
        }

        [Fact]
        public void Resolver_DesdeLaRaizConValidacionDeScopes_LanzaPorqueSonScoped()
        {
            using var provider = CreateServices().AddEtlApplicationServices().BuildServiceProvider(validateScopes: true);

            Action act = () => provider.GetRequiredService<IRunEtlProcessUseCase>();

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Resolver_SinLasDependenciasDeInfraestructura_LanzaInvalidOperationException()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddEtlApplicationServices();
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();

            Action runEtl = () => scope.ServiceProvider.GetRequiredService<IRunEtlProcessUseCase>();
            Action runLogs = () => scope.ServiceProvider.GetRequiredService<IRunLogsEtlProcessUseCase>();
            Action purge = () => scope.ServiceProvider.GetRequiredService<IPurgeEtlJobControlUseCase>();

            runEtl.Should().Throw<InvalidOperationException>().WithMessage("*IExternalDataGateway*");
            runLogs.Should().Throw<InvalidOperationException>().WithMessage("*IExternalDataGateway*");
            purge.Should().Throw<InvalidOperationException>().WithMessage("*IUnitOfWork*");
        }

        [Fact]
        public void Resolver_SinUnaDependenciaPuntual_LanzaIndicandoCualFalta()
        {
            var services = CreateServices();
            services.Remove(services.Single(d => d.ServiceType == typeof(IEtlChangeMessageCatalog)));
            services.AddEtlApplicationServices();
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();

            Action act = () => scope.ServiceProvider.GetRequiredService<IRunEtlProcessUseCase>();

            act.Should().Throw<InvalidOperationException>().WithMessage("*IEtlChangeMessageCatalog*");
        }

        // ---------- ResolveGlobalPageSize (vía el page_size que cada caso de uso registra en etl_job_control) ----------

        [Theory]
        [InlineData(false, 500, null)]   // paginación deshabilitada -> sin paginación aunque haya PageSize
        [InlineData(false, 0, null)]
        [InlineData(true, 500, 500)]     // habilitada y válida
        [InlineData(true, 1, 1)]
        [InlineData(true, 0, null)]      // habilitada pero PageSize 0 -> sin paginación
        [InlineData(true, -10, null)]    // habilitada pero PageSize negativo -> sin paginación
        public async Task RunLogsEtlProcessUseCase_PageSizeConfigurado_SeRegistraElPageSizeEfectivo(Boolean enabled, Int32 configuredPageSize, Int32? expectedPageSize)
        {
            using var provider = CreateServices(enabled, configuredPageSize).AddEtlApplicationServices().BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var useCase = scope.ServiceProvider.GetRequiredService<IRunLogsEtlProcessUseCase>();

            var result = await useCase.ExecuteAsync();

            result.Success.Should().BeTrue();
            _jobControlRepository.Verify(
                r => r.StartRunAsync(EtlJobName.LogStatusTracking, expectedPageSize, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(false, 500, null)]
        [InlineData(true, 250, 250)]
        [InlineData(true, 0, null)]
        [InlineData(true, -1, null)]
        public async Task RunEtlProcessUseCase_PageSizeConfigurado_SeRegistraElMismoPageSizeEnAmbosJobs(Boolean enabled, Int32 configuredPageSize, Int32? expectedPageSize)
        {
            using var provider = CreateServices(enabled, configuredPageSize).AddEtlApplicationServices().BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var useCase = scope.ServiceProvider.GetRequiredService<IRunEtlProcessUseCase>();

            var result = await useCase.ExecuteAsync();

            result.Success.Should().BeTrue();
            _jobControlRepository.Verify(
                r => r.StartRunAsync(EtlJobName.ApplicationDataSheet, expectedPageSize, It.IsAny<CancellationToken>()), Times.Once);
            _jobControlRepository.Verify(
                r => r.StartRunAsync(EtlJobName.ApplicationDataSheetMigration, expectedPageSize, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RunLogsEtlProcessUseCase_ConsultaUnicamenteLaApiDatalogs()
        {
            using var provider = CreateServices().AddEtlApplicationServices().BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var useCase = scope.ServiceProvider.GetRequiredService<IRunLogsEtlProcessUseCase>();

            await useCase.ExecuteAsync();

            _gateway.Verify(g => g.FetchDataPagedAsync("DATALOGS", It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>()), Times.Once);
            _gateway.Verify(g => g.FetchDataPagedAsync(It.Is<String>(n => n != "DATALOGS"), It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RunEtlProcessUseCase_ConsultaLasCincoApisOperativasYNoDatalogs()
        {
            using var provider = CreateServices().AddEtlApplicationServices().BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var useCase = scope.ServiceProvider.GetRequiredService<IRunEtlProcessUseCase>();

            await useCase.ExecuteAsync();

            foreach (var api in new[] { "BPMS", "SIM", "OPENCOMEX", "ASISCOMEX", "SYSTEMCARRIER" })
            {
                _gateway.Verify(g => g.FetchDataPagedAsync(api, It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>()), Times.Once);
            }
            _gateway.Verify(g => g.FetchDataPagedAsync("DATALOGS", It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // ---------- Retención de etl_job_control ----------

        [Theory]
        [InlineData(30)]
        [InlineData(7)]
        [InlineData(0)]
        public async Task PurgeEtlJobControlUseCase_DiasDeRetencionConfigurados_SeUsanEnLaDepuracion(Int32 retentionDays)
        {
            _jobControlRepository
                .Setup(r => r.DeleteOlderThanAsync(EtlJobName.ApplicationDataSheet, retentionDays, It.IsAny<CancellationToken>()))
                .ReturnsAsync(12);
            using var provider = CreateServices(retentionDays: retentionDays).AddEtlApplicationServices().BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var useCase = scope.ServiceProvider.GetRequiredService<IPurgeEtlJobControlUseCase>();

            Int32 deleted = await useCase.ExecuteAsync();

            deleted.Should().Be(12);
            _jobControlRepository.Verify(
                r => r.DeleteOlderThanAsync(EtlJobName.ApplicationDataSheet, retentionDays, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PurgeEtlJobControlUseCase_ElRepositorioFalla_DevuelveCeroSinLanzar()
        {
            _jobControlRepository
                .Setup(r => r.DeleteOlderThanAsync(It.IsAny<EtlJobName>(), It.IsAny<Int32>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("BD caída"));
            using var provider = CreateServices().AddEtlApplicationServices().BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var useCase = scope.ServiceProvider.GetRequiredService<IPurgeEtlJobControlUseCase>();

            Int32 deleted = await useCase.ExecuteAsync();

            deleted.Should().Be(0);
        }

        // ---------- Usuario de sistema del seguimiento de cambios ----------

        [Fact]
        public async Task RunEtlProcessUseCase_CambioDeEstado_RegistraElUsuarioDeSistemaConfigurado()
        {
            // Migración inicial ya completada -> la corrida detecta y notifica cambios.
            _jobControlRepository
                .Setup(r => r.HasCompletedRunAsync(EtlJobName.ApplicationDataSheetMigration, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var sheet = new ApplicationDataSheet { DocumentoTransporteHbl = "HBL-1", NitCliente = "900", Estado = "NUEVO" };
            var unified = new DynamicDataSet(new[] { "ESTADO" }, new[] { new DynamicRecord(new Dictionary<String, String> { ["ESTADO"] = "NUEVO" }) });
            _gateway
                .Setup(g => g.FetchDataPagedAsync("BPMS", It.IsAny<IDictionary<String, String>>(), It.IsAny<CancellationToken>()))
                .Returns(() => OnePage(unified));
            _merger
                .Setup(m => m.Merge(It.IsAny<IEnumerable<DynamicDataSet>>(), It.IsAny<String>(), It.IsAny<DataSetJoinType>()))
                .Returns(unified);
            _shipmentsMapping.Setup(m => m.Map(unified)).Returns(new List<ApplicationDataSheet> { sheet });

            var repository = new Mock<IApplicationDataSheetRepository>();
            repository.Setup(r => r.GetChangeSnapshotsAsync(It.IsAny<IEnumerable<String>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<String, ApplicationDataSheetChangeSnapshot>());
            repository.Setup(r => r.UpsertBatchAsync(It.IsAny<IEnumerable<ApplicationDataSheet>>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
            repository.Setup(r => r.GetIdsByDocumentAsync(It.IsAny<IEnumerable<String>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<String, Int64> { ["HBL-1"] = 5L });
            var logRepository = new Mock<ILogStatusTrackingRepository>();
            IEnumerable<LogStatusTracking>? inserted = null;
            logRepository.Setup(r => r.InsertBatchAsync(It.IsAny<IEnumerable<LogStatusTracking>>(), It.IsAny<CancellationToken>()))
                .Callback<IEnumerable<LogStatusTracking>, CancellationToken>((rows, _) => inserted = rows.ToList())
                .ReturnsAsync(1);
            var outboxRepository = new Mock<IOutboxMessageRepository>();
            outboxRepository.Setup(r => r.InsertBatchAsync(It.IsAny<IEnumerable<OutboxMessage>>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _unitOfWork.Setup(u => u.GetRepository<IApplicationDataSheetRepository>()).Returns(repository.Object);
            _unitOfWork.Setup(u => u.GetRepository<ILogStatusTrackingRepository>()).Returns(logRepository.Object);
            _unitOfWork.Setup(u => u.GetRepository<IOutboxMessageRepository>()).Returns(outboxRepository.Object);

            _changeDetector
                .Setup(d => d.DetectChanges(It.IsAny<IReadOnlyList<ApplicationDataSheet>>(), It.IsAny<IReadOnlyDictionary<String, ApplicationDataSheetChangeSnapshot>>()))
                .Returns(new List<ApplicationDataSheetChange>
                {
                    new() { DocumentoTransporteHbl = "HBL-1", NitCliente = "900", IsNewDocument = true, StateChanged = true, EstadoAnterior = "", NuevoEstado = "NUEVO" }
                });
            _messageCatalog.Setup(c => c.GetStateChangeMessage(It.IsAny<String>())).Returns(("titulo", "mensaje"));

            using var provider = CreateServices(systemUser: "USUARIO_SISTEMA_PRUEBA").AddEtlApplicationServices().BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var useCase = scope.ServiceProvider.GetRequiredService<IRunEtlProcessUseCase>();

            var result = await useCase.ExecuteAsync();

            result.Success.Should().BeTrue(result.ErrorMessage);
            result.StateChangesDetected.Should().Be(1);
            inserted.Should().NotBeNull();
            inserted!.Should().ContainSingle().Which.UsuarioCambio.Should().Be("USUARIO_SISTEMA_PRUEBA");
        }

        private static async IAsyncEnumerable<DynamicDataSet> OnePage(DynamicDataSet page)
        {
            await Task.CompletedTask;
            yield return page;
        }
    }
}
