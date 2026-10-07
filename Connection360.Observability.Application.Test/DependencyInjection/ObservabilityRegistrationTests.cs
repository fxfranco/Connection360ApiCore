using System.Diagnostics;
using Connection360.Observability.Application.DependencyInjection;
using Connection360.Observability.Application.Pipeline;
using Connection360.Observability.Application.Test.Support;
using Connection360.Observability.Domain.Models;
using Connection360.Observability.Domain.Ports;
using Connection360.Observability.Domain.Settings;
using Connection360.Observability.Domain.Telemetry;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Connection360.Observability.Application.Test.DependencyInjection
{
    public class ObservabilityRegistrationTests
    {
        private sealed class AllSignalsStore : ILogStore, IMetricStore, ITraceStore, ITelemetryStoreInitializer
        {
            public InMemoryStore<LogRecord> Logs { get; } = new("todo");
            public InMemoryStore<MetricRecord> Metrics { get; } = new("todo");
            public InMemoryStore<TraceRecord> Traces { get; } = new("todo");
            public Boolean Initialized { get; private set; }
            public String Name => "todo";
            public Task WriteBatchAsync(IReadOnlyList<LogRecord> records, CancellationToken cancellationToken) => Logs.WriteBatchAsync(records, cancellationToken);
            public Task WriteBatchAsync(IReadOnlyList<MetricRecord> records, CancellationToken cancellationToken) => Metrics.WriteBatchAsync(records, cancellationToken);
            public Task WriteBatchAsync(IReadOnlyList<TraceRecord> records, CancellationToken cancellationToken) => Traces.WriteBatchAsync(records, cancellationToken);
            public Task InitializeAsync(CancellationToken cancellationToken) { Initialized = true; return Task.CompletedTask; }
        }

        private sealed class LogsOnlyStore : ILogStore
        {
            public String Name => "solo-logs";
            public Task WriteBatchAsync(IReadOnlyList<LogRecord> records, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private static IConfiguration Config(params (String Key, String? Value)[] values)
            => new ConfigurationBuilder().AddInMemoryCollection(values.Select(v => new KeyValuePair<String, String?>(v.Key, v.Value))).Build();

        private static ServiceCollection Services()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            return services;
        }

        [Fact]
        public void AddConnection360Observability_ConNombreVacio_Lanza()
        {
            Action act = () => Services().AddConnection360Observability(Config(), " ");

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void AddConnection360Observability_Habilitada_RegistraIdentidadColasYComponentes()
        {
            var services = Services();

            var builder = services.AddConnection360Observability(Config(("ASPNETCORE_ENVIRONMENT", "Staging")), ObservedServices.ApiCore);
            using ServiceProvider provider = services.BuildServiceProvider();

            builder.Enabled.Should().BeTrue();
            ServiceIdentity identity = provider.GetRequiredService<ServiceIdentity>();
            identity.ServiceName.Should().Be("ApiCore");
            identity.Environment.Should().Be("Staging");
            identity.InstanceId.Should().Contain(":");
            identity.ServiceVersion.Should().NotBeNullOrWhiteSpace();
            provider.GetRequiredService<TelemetryQueue<LogRecord>>().Should().NotBeNull();
            provider.GetRequiredService<TelemetryQueue<MetricRecord>>().Should().NotBeNull();
            provider.GetRequiredService<TelemetryQueue<Activity>>().Should().NotBeNull();
            provider.GetServices<IObservabilityComponent>().Should().HaveCount(6);
            provider.GetServices<IHostedService>().Should().HaveCount(6);
            provider.GetServices<ILoggerProvider>().Should().ContainSingle(p => p.GetType().Name == "TelemetryLoggerProvider");
        }

        [Fact]
        public void AddConnection360Observability_ElMismoComponenteEsHostedServiceYObservabilityComponent()
        {
            var services = Services();
            services.AddConnection360Observability(Config(), ObservedServices.Etl);
            using ServiceProvider provider = services.BuildServiceProvider();

            List<Object> hosted = provider.GetServices<IHostedService>().Cast<Object>().ToList();
            List<Object> components = provider.GetServices<IObservabilityComponent>().Cast<Object>().ToList();

            hosted.Should().HaveCount(components.Count);
            for (Int32 i = 0; i < hosted.Count; i++)
            {
                hosted[i].Should().BeSameAs(components[i]);
            }
        }

        [Fact]
        public void AddConnection360Observability_Deshabilitada_NoRegistraComponentesNiProveedorDeLogs()
        {
            var services = Services();

            var builder = services.AddConnection360Observability(Config(("Observability:Enabled", "false")), ObservedServices.ApiCore);
            using ServiceProvider provider = services.BuildServiceProvider();

            builder.Enabled.Should().BeFalse();
            provider.GetServices<IObservabilityComponent>().Should().BeEmpty();
            provider.GetServices<IHostedService>().Should().BeEmpty();
            provider.GetServices<ILoggerProvider>().Should().BeEmpty();
            provider.GetService<ServiceIdentity>().Should().BeNull();
            provider.GetRequiredService<IOptions<ObservabilityOptions>>().Value.Enabled.Should().BeFalse();
        }

        [Fact]
        public void AddConnection360Observability_ConPilaresDeshabilitados_OmiteSusComponentes()
        {
            var services = Services();
            services.AddConnection360Observability(Config(
                ("Observability:Logs:Enabled", "false"),
                ("Observability:Traces:Enabled", "false")), ObservedServices.ApiNotification);
            using ServiceProvider provider = services.BuildServiceProvider();

            // inicializador + escritor de métricas + recolector de métricas
            provider.GetServices<IObservabilityComponent>().Should().HaveCount(3);
            provider.GetServices<ILoggerProvider>().Should().BeEmpty();
        }

        [Fact]
        public void AddConnection360Observability_NormalizaValoresInvalidos()
        {
            var services = Services();
            services.AddConnection360Observability(Config(
                ("Observability:ShutdownTimeoutSeconds", "-1"),
                ("Observability:MaxFieldLength", "0"),
                ("Observability:Logs:QueueCapacity", "0"),
                ("Observability:Logs:BatchSize", "-3"),
                ("Observability:Metrics:CollectionIntervalSeconds", "0"),
                ("Observability:Metrics:MaxSeriesPerInstrument", "-1"),
                ("Observability:Traces:SamplingRatio", "7.5")), ObservedServices.ApiCore);
            using ServiceProvider provider = services.BuildServiceProvider();

            ObservabilityOptions options = provider.GetRequiredService<IOptions<ObservabilityOptions>>().Value;

            options.ShutdownTimeoutSeconds.Should().Be(10);
            options.MaxFieldLength.Should().Be(4096);
            options.Logs.QueueCapacity.Should().Be(10_000);
            options.Logs.BatchSize.Should().Be(200);
            options.Metrics.CollectionIntervalSeconds.Should().Be(30);
            options.Metrics.MaxSeriesPerInstrument.Should().Be(500);
            options.Traces.SamplingRatio.Should().Be(1.0);
        }

        [Fact]
        public void AddConnection360Observability_LeeLaConfiguracionDeLaSeccion()
        {
            var services = Services();
            services.AddConnection360Observability(Config(
                ("Observability:Logs:MinimumLevel", "Warning"),
                ("Observability:Traces:SamplingRatio", "0.25"),
                ("Observability:Traces:ExcludePaths:0", "/ping")), ObservedServices.ApiCore);
            using ServiceProvider provider = services.BuildServiceProvider();

            ObservabilityOptions options = provider.GetRequiredService<IOptions<ObservabilityOptions>>().Value;

            options.Logs.MinimumLevel.Should().Be("Warning");
            options.Traces.SamplingRatio.Should().Be(0.25);
            options.Traces.ExcludePaths.Should().Contain("/ping");
        }

        [Fact]
        public void AddStore_ConUnaClaseDeTresContratos_LaRegistraParaCadaSenal()
        {
            var services = Services();
            services.AddConnection360Observability(Config(), ObservedServices.ApiCore).AddStore<AllSignalsStore>();
            using ServiceProvider provider = services.BuildServiceProvider();

            AllSignalsStore single = provider.GetRequiredService<AllSignalsStore>();

            provider.GetServices<ILogStore>().Should().ContainSingle().Which.Should().BeSameAs(single);
            provider.GetServices<IMetricStore>().Should().ContainSingle().Which.Should().BeSameAs(single);
            provider.GetServices<ITraceStore>().Should().ContainSingle().Which.Should().BeSameAs(single);
            provider.GetServices<ITelemetryStoreInitializer>().Should().ContainSingle().Which.Should().BeSameAs(single);
        }

        [Fact]
        public void AddStore_ConUnaClaseDeUnSoloContrato_SoloLaRegistraParaEse()
        {
            var services = Services();
            services.AddConnection360Observability(Config(), ObservedServices.ApiCore).AddStore<LogsOnlyStore>();
            using ServiceProvider provider = services.BuildServiceProvider();

            provider.GetServices<ILogStore>().Should().ContainSingle();
            provider.GetServices<IMetricStore>().Should().BeEmpty();
            provider.GetServices<ITraceStore>().Should().BeEmpty();
            provider.GetServices<ITelemetryStoreInitializer>().Should().BeEmpty();
        }

        [Fact]
        public void AddStore_ConLaObservabilidadDeshabilitada_NoRegistraNada()
        {
            var services = Services();
            services.AddConnection360Observability(Config(("Observability:Enabled", "false")), ObservedServices.ApiCore).AddStore<AllSignalsStore>();
            using ServiceProvider provider = services.BuildServiceProvider();

            provider.GetService<AllSignalsStore>().Should().BeNull();
            provider.GetServices<ILogStore>().Should().BeEmpty();
        }

        [Fact]
        public void AddStore_VariosAlmacenamientos_QuedanTodosRegistrados()
        {
            var services = Services();
            services.AddConnection360Observability(Config(), ObservedServices.ApiCore).AddStore<AllSignalsStore>().AddStore<LogsOnlyStore>();
            using ServiceProvider provider = services.BuildServiceProvider();

            provider.GetServices<ILogStore>().Should().HaveCount(2);
        }

        [Fact]
        public async Task StartObservabilityAsync_ExtremoAExtremo_LlevaLogsMetricasYTrazasAlAlmacenamientoYLosVaciaAlTerminar()
        {
            var store = new AllSignalsStore();
            var services = Services();
            services.AddConnection360Observability(
                Config(("Observability:Metrics:IncludeMeters:0", Connection360Telemetry.Name), ("Observability:Logs:FlushIntervalSeconds", "60"), ("Observability:Traces:FlushIntervalSeconds", "60")),
                ObservedServices.Etl);
            services.AddSingleton(store);
            services.AddSingleton<ILogStore>(store);
            services.AddSingleton<IMetricStore>(store);
            services.AddSingleton<ITraceStore>(store);
            services.AddSingleton<ITelemetryStoreInitializer>(store);
            await using ServiceProvider provider = services.BuildServiceProvider();
            Counter_Name = "e2e.contador." + Guid.NewGuid();

            await using (await provider.StartObservabilityAsync())
            {
                provider.GetRequiredService<ILoggerFactory>().CreateLogger("Etl.Prueba").LogInformation("hola {Valor}", 1);
                Connection360Telemetry.Meter.CreateCounter<Int32>(Counter_Name).Add(3);
                using (Connection360Telemetry.Source.StartActivity("e2e.operacion")) { }
            }

            await Wait.UntilAsync(() => store.Initialized, "el inicializador se ejecutó en segundo plano");
            store.Logs.Records.Should().Contain(r => r.Message == "hola 1" && r.Service == "Etl");
            store.Metrics.Records.Should().Contain(r => r.Name == Counter_Name && r.Sum == 3 && r.Service == "Etl");
            store.Traces.Records.Should().Contain(r => r.Name == "e2e.operacion" && r.Service == "Etl");
        }

        private static String Counter_Name = String.Empty;

        [Fact]
        public async Task StartObservabilityAsync_SinComponentes_DevuelveUnLifetimeInofensivo()
        {
            var services = Services();
            services.AddConnection360Observability(Config(("Observability:Enabled", "false")), ObservedServices.Etl);
            await using ServiceProvider provider = services.BuildServiceProvider();

            Func<Task> act = async () => { await using var lifetime = await provider.StartObservabilityAsync(); };

            await act.Should().NotThrowAsync();
        }
    }
}
