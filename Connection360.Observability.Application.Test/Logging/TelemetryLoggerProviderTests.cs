using System.Diagnostics;
using Connection360.Observability.Application.Logging;
using Connection360.Observability.Application.Pipeline;
using Connection360.Observability.Application.Test.Support;
using Connection360.Observability.Domain.Models;
using Connection360.Observability.Domain.Settings;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Connection360.Observability.Application.Test.Logging
{
    public class TelemetryLoggerProviderTests
    {
        private static readonly ServiceIdentity Identity = new("ApiNotification", "1.2.3", "Testing", "equipo:1");

        private sealed class Fixture : IDisposable
        {
            public Fixture(Action<ObservabilityOptions>? configure = null)
            {
                var options = new ObservabilityOptions();
                options.Logs.CategoryLevels.Clear();
                configure?.Invoke(options);
                Queue = new TelemetryQueue<LogRecord>("logs", 1000);
                Provider = new TelemetryLoggerProvider(Queue, Identity, options);
                Factory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(Provider));
            }

            public TelemetryQueue<LogRecord> Queue { get; }
            public TelemetryLoggerProvider Provider { get; }
            public ILoggerFactory Factory { get; }
            public ILogger Logger(String category = "Pruebas.Categoria") => Factory.CreateLogger(category);
            public void Dispose() => Factory.Dispose();
        }

        [Fact]
        public void Log_ConPlantilla_GeneraRegistroEstructurado()
        {
            using var fixture = new Fixture();

            fixture.Logger().LogInformation("Procesadas {Cantidad} notificaciones de {Cliente}", 5, "ACME");

            LogRecord record = fixture.Queue.DrainAll().Should().ContainSingle().Subject;
            record.Message.Should().Be("Procesadas 5 notificaciones de ACME");
            record.MessageTemplate.Should().Be("Procesadas {Cantidad} notificaciones de {Cliente}");
            record.Level.Should().Be("Information");
            record.SeverityNumber.Should().Be(9);
            record.Category.Should().Be("Pruebas.Categoria");
            record.Attributes["Cantidad"].Should().Be(5);
            record.Attributes["Cliente"].Should().Be("ACME");
            record.Attributes.Should().NotContainKey("{OriginalFormat}");
        }

        [Fact]
        public void Log_IncluyeIdentidadDelServicio()
        {
            using var fixture = new Fixture();

            fixture.Logger().LogWarning("algo");

            LogRecord record = fixture.Queue.DrainAll().Single();
            record.Service.Should().Be("ApiNotification");
            record.ServiceVersion.Should().Be("1.2.3");
            record.Environment.Should().Be("Testing");
            record.InstanceId.Should().Be("equipo:1");
            record.Timestamp.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Theory]
        [InlineData(LogLevel.Trace, 1)]
        [InlineData(LogLevel.Debug, 5)]
        [InlineData(LogLevel.Information, 9)]
        [InlineData(LogLevel.Warning, 13)]
        [InlineData(LogLevel.Error, 17)]
        [InlineData(LogLevel.Critical, 21)]
        public void Log_AsignaElNumeroDeSeveridadDeOpenTelemetry(LogLevel level, Int32 expected)
        {
            using var fixture = new Fixture(o => o.Logs.MinimumLevel = "Trace");

            fixture.Logger().Log(level, "x");

            fixture.Queue.DrainAll().Single().SeverityNumber.Should().Be(expected);
        }

        [Fact]
        public void Log_ConExcepcion_RegistraTipoMensajeYPila()
        {
            using var fixture = new Fixture();

            fixture.Logger().LogError(new InvalidOperationException("falló"), "Error al procesar");

            LogRecord record = fixture.Queue.DrainAll().Single();
            record.ExceptionType.Should().Be(typeof(InvalidOperationException).FullName);
            record.ExceptionMessage.Should().Be("falló");
            record.ExceptionStackTrace.Should().Contain("InvalidOperationException");
        }

        [Fact]
        public void Log_SinExcepcion_DejaLosCamposDeExcepcionNulos()
        {
            using var fixture = new Fixture();

            fixture.Logger().LogInformation("ok");

            LogRecord record = fixture.Queue.DrainAll().Single();
            record.ExceptionType.Should().BeNull();
            record.ExceptionMessage.Should().BeNull();
            record.ExceptionStackTrace.Should().BeNull();
        }

        [Fact]
        public void Log_ConEventId_RegistraIdYNombre()
        {
            using var fixture = new Fixture();

            fixture.Logger().LogInformation(new EventId(42, "Evento"), "x");

            LogRecord record = fixture.Queue.DrainAll().Single();
            record.EventId.Should().Be(42);
            record.EventName.Should().Be("Evento");
        }

        [Fact]
        public void Log_ConScope_AgregaLosValoresDelScopeALosAtributos()
        {
            using var fixture = new Fixture();
            ILogger logger = fixture.Logger();

            using (logger.BeginScope(new Dictionary<String, Object?> { ["RequestId"] = "abc", ["UserId"] = 7 }))
            {
                logger.LogInformation("dentro");
            }

            logger.LogInformation("fuera");
            List<LogRecord> records = fixture.Queue.DrainAll();
            records[0].Attributes["RequestId"].Should().Be("abc");
            records[0].Attributes["UserId"].Should().Be(7);
            records[1].Attributes.Should().NotContainKey("RequestId");
        }

        [Fact]
        public void Log_LosValoresDelMensajePrevalecenSobreLosDelScope()
        {
            using var fixture = new Fixture();
            ILogger logger = fixture.Logger();

            using (logger.BeginScope(new Dictionary<String, Object?> { ["Cliente"] = "del scope" }))
            {
                logger.LogInformation("Hola {Cliente}", "del mensaje");
            }

            fixture.Queue.DrainAll().Single().Attributes["Cliente"].Should().Be("del mensaje");
        }

        [Fact]
        public void Log_DentroDeUnActivity_CorrelacionaTraceIdYSpanId()
        {
            using var fixture = new Fixture();
            using var source = new ActivitySource("test.logger." + Guid.NewGuid());
            using var listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == source.Name,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            };
            ActivitySource.AddActivityListener(listener);

            using Activity activity = source.StartActivity("operacion")!;
            fixture.Logger().LogInformation("con traza");

            LogRecord record = fixture.Queue.DrainAll().Single();
            record.TraceId.Should().Be(activity.TraceId.ToHexString());
            record.SpanId.Should().Be(activity.SpanId.ToHexString());
        }

        [Fact]
        public void Log_SinActivity_NoTrazaNiSpan()
        {
            using var fixture = new Fixture();
            Activity.Current = null;

            fixture.Logger().LogInformation("sin traza");

            LogRecord record = fixture.Queue.DrainAll().Single();
            record.TraceId.Should().BeNull();
            record.SpanId.Should().BeNull();
        }

        [Fact]
        public void Log_MensajeMasLargoQueElMaximo_SeTrunca()
        {
            using var fixture = new Fixture(o => o.MaxFieldLength = 20);

            fixture.Logger().LogInformation("Valor {Texto}", new String('x', 500));

            LogRecord record = fixture.Queue.DrainAll().Single();
            record.Message.Length.Should().Be(20);
            ((String)record.Attributes["Texto"]!).Length.Should().Be(20);
        }

        [Fact]
        public void Log_ConNivelPorDebajoDelMinimo_NoSeCaptura()
        {
            using var fixture = new Fixture(o => o.Logs.MinimumLevel = "Warning");

            fixture.Logger().LogInformation("ignorado");
            fixture.Logger().LogWarning("capturado");

            fixture.Queue.DrainAll().Select(r => r.Message).Should().Equal("capturado");
        }

        [Fact]
        public void Log_ConNivelMinimoInvalido_UsaInformation()
        {
            using var fixture = new Fixture(o => o.Logs.MinimumLevel = "NoExiste");

            fixture.Logger().LogDebug("debug");
            fixture.Logger().LogInformation("info");

            fixture.Queue.DrainAll().Select(r => r.Message).Should().Equal("info");
        }

        [Fact]
        public void Log_NivelPorCategoria_GanaElPrefijoMasEspecifico()
        {
            using var fixture = new Fixture(o =>
            {
                o.Logs.MinimumLevel = "Information";
                o.Logs.CategoryLevels["Microsoft"] = "Warning";
                o.Logs.CategoryLevels["Microsoft.Hosting"] = "Information";
            });

            fixture.Logger("Microsoft.AspNetCore.Routing").LogInformation("ruido");
            fixture.Logger("Microsoft.Hosting.Lifetime").LogInformation("arranque");
            fixture.Logger("Microsoft.AspNetCore.Routing").LogWarning("alerta");

            fixture.Queue.DrainAll().Select(r => r.Message).Should().Equal("arranque", "alerta");
        }

        [Theory]
        [InlineData("Connection360.Observability.LogsWriter")]
        [InlineData("MongoDB.Driver")]
        public void CreateLogger_ConCategoriaInterna_NoCapturaNada(String category)
        {
            using var fixture = new Fixture();

            ILogger logger = fixture.Provider.CreateLogger(category);
            logger.LogError("no debe capturarse");

            logger.IsEnabled(LogLevel.Critical).Should().BeFalse();
            fixture.Queue.DrainAll().Should().BeEmpty();
        }

        [Fact]
        public void CreateLogger_ConLogsDeshabilitados_NoCapturaNada()
        {
            using var fixture = new Fixture(o => o.Logs.Enabled = false);

            fixture.Logger().LogCritical("x");

            fixture.Queue.DrainAll().Should().BeEmpty();
        }

        [Fact]
        public void Log_ConLaColaLlena_NoLanzaExcepcion()
        {
            var options = new ObservabilityOptions();
            var queue = new TelemetryQueue<LogRecord>("logs", 1);
            var provider = new TelemetryLoggerProvider(queue, Identity, options);
            ILogger logger = provider.CreateLogger("Pruebas");

            Action act = () =>
            {
                for (Int32 i = 0; i < 10; i++) logger.LogInformation("m{i}", i);
            };

            act.Should().NotThrow();
            queue.Dropped.Should().Be(9);
        }

        [Fact]
        public void Log_ConUnFormateadorQueFalla_NoPropagaLaExcepcion()
        {
            using var fixture = new Fixture();
            ILogger logger = fixture.Logger();

            Action act = () => logger.Log(LogLevel.Information, new EventId(1), "estado", null, (_, _) => throw new InvalidOperationException("boom"));

            act.Should().NotThrow();
        }

        [Fact]
        public void IsEnabled_NoneNuncaEstaHabilitado()
        {
            using var fixture = new Fixture();

            fixture.Logger().IsEnabled(LogLevel.None).Should().BeFalse();
        }
    }
}
