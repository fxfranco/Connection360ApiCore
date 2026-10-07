using Connection360.Observability.Domain.Models;
using Connection360.Observability.Infrastructure.Mongo.Persistence;
using FluentAssertions;
using MongoDB.Bson;
using Xunit;

namespace Connection360.Observability.Infrastructure.Mongo.Test.Persistence
{
    public class TelemetryBsonMapperTests
    {
        private static readonly DateTime Now = new(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc);

        private static LogRecord Log(Action<Dictionary<String, Object?>>? attributes = null, String? exceptionType = null)
        {
            var attrs = new Dictionary<String, Object?>();
            attributes?.Invoke(attrs);
            return new LogRecord
            {
                Timestamp = Now, Service = "ApiNotification", ServiceVersion = "1.0", Environment = "Dev", InstanceId = "pc:1",
                Level = "Error", SeverityNumber = 17, Category = "Cat", EventId = 5, EventName = "Evt", Message = "msg", MessageTemplate = "tpl",
                Attributes = attrs, ExceptionType = exceptionType, ExceptionMessage = exceptionType is null ? null : "m", ExceptionStackTrace = exceptionType is null ? null : "s",
                TraceId = "t", SpanId = "s1",
            };
        }

        [Fact]
        public void Log_MapeaCamposComunesYPropios()
        {
            BsonDocument doc = TelemetryBsonMapper.ToDocument(Log());

            doc["service"].AsString.Should().Be("ApiNotification");
            doc["serviceVersion"].AsString.Should().Be("1.0");
            doc["environment"].AsString.Should().Be("Dev");
            doc["instanceId"].AsString.Should().Be("pc:1");
            doc["timestamp"].IsBsonDateTime.Should().BeTrue();
            doc["timestamp"].ToUniversalTime().Should().Be(Now);
            doc["level"].AsString.Should().Be("Error");
            doc["severityNumber"].AsInt32.Should().Be(17);
            doc["category"].AsString.Should().Be("Cat");
            doc["eventId"].AsInt32.Should().Be(5);
            doc["eventName"].AsString.Should().Be("Evt");
            doc["message"].AsString.Should().Be("msg");
            doc["messageTemplate"].AsString.Should().Be("tpl");
            doc["traceId"].AsString.Should().Be("t");
            doc["spanId"].AsString.Should().Be("s1");
        }

        [Fact]
        public void Log_SinExcepcion_GuardaNulo() => TelemetryBsonMapper.ToDocument(Log())["exception"].IsBsonNull.Should().BeTrue();

        [Fact]
        public void Log_ConExcepcion_GuardaSubdocumento()
        {
            BsonDocument exception = TelemetryBsonMapper.ToDocument(Log(exceptionType: "System.Boom"))["exception"].AsBsonDocument;

            exception["type"].AsString.Should().Be("System.Boom");
            exception["message"].AsString.Should().Be("m");
            exception["stackTrace"].AsString.Should().Be("s");
        }

        [Fact]
        public void Log_CamposOpcionalesNulos_SeGuardanComoNulo()
        {
            var record = new LogRecord { Timestamp = Now, Service = "Etl", Level = "Information", Message = "x" };

            BsonDocument doc = TelemetryBsonMapper.ToDocument(record);

            doc["eventName"].IsBsonNull.Should().BeTrue();
            doc["messageTemplate"].IsBsonNull.Should().BeTrue();
            doc["traceId"].IsBsonNull.Should().BeTrue();
            doc["spanId"].IsBsonNull.Should().BeTrue();
        }

        [Fact]
        public void Atributos_ConservanTiposSimples()
        {
            var when = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            BsonDocument doc = TelemetryBsonMapper.Attributes(new Dictionary<String, Object?>
            {
                ["texto"] = "a", ["entero"] = 1, ["largo"] = 2L, ["doble"] = 1.5d, ["flotante"] = 2.5f, ["bool"] = true,
                ["corto"] = (Int16)3, ["byte"] = (Byte)4, ["fecha"] = when, ["nulo"] = null,
            });

            doc["texto"].AsString.Should().Be("a");
            doc["entero"].AsInt32.Should().Be(1);
            doc["largo"].AsInt64.Should().Be(2);
            doc["doble"].AsDouble.Should().Be(1.5);
            doc["flotante"].AsDouble.Should().Be(2.5);
            doc["bool"].AsBoolean.Should().BeTrue();
            doc["corto"].AsInt32.Should().Be(3);
            doc["byte"].AsInt32.Should().Be(4);
            doc["fecha"].ToUniversalTime().Should().Be(when);
            doc["nulo"].IsBsonNull.Should().BeTrue();
        }

        [Fact]
        public void Atributos_ConNombresConPuntoDeOpenTelemetry_ConservanLaClave()
        {
            BsonDocument doc = TelemetryBsonMapper.Attributes(new Dictionary<String, Object?> { ["http.request.method"] = "GET" });

            doc.Contains("http.request.method").Should().BeTrue();
        }

        [Fact]
        public void Atributos_ConClaveQueEmpiezaConDolar_LaPrefijaConGuionBajo()
        {
            BsonDocument doc = TelemetryBsonMapper.Attributes(new Dictionary<String, Object?> { ["$set"] = 1, ["$"] = 2 });

            doc.Contains("_set").Should().BeTrue();
            doc.Contains("$set").Should().BeFalse();
            doc.Contains("_").Should().BeTrue("una clave que solo es '$' queda como '_'");
        }

        [Fact]
        public void Atributos_ConClaveVacia_LaOmite()
        {
            BsonDocument doc = TelemetryBsonMapper.Attributes(new Dictionary<String, Object?> { [""] = 1, ["ok"] = 2 });

            doc.Names.Should().Equal("ok");
        }

        [Fact]
        public void Atributos_DoblesNoFinitos_SeGuardanComoCero()
        {
            BsonDocument doc = TelemetryBsonMapper.Attributes(new Dictionary<String, Object?> { ["nan"] = Double.NaN, ["inf"] = Double.PositiveInfinity });

            doc["nan"].AsDouble.Should().Be(0);
            doc["inf"].AsDouble.Should().Be(0);
        }

        [Fact]
        public void Atributos_Listas_SeGuardanComoArreglo()
        {
            BsonDocument doc = TelemetryBsonMapper.Attributes(new Dictionary<String, Object?> { ["lista"] = new List<Object?> { 1, "dos", null } });

            doc["lista"].AsBsonArray.Select(v => v.BsonType).Should().Equal(BsonType.Int32, BsonType.String, BsonType.Null);
        }

        [Fact]
        public void Atributos_ObjetoDesconocido_SeGuardaComoTexto()
        {
            BsonDocument doc = TelemetryBsonMapper.Attributes(new Dictionary<String, Object?> { ["uri"] = new Uri("http://x/") });

            doc["uri"].AsString.Should().Be("http://x/");
        }

        [Fact]
        public void Fecha_SinKind_SeTrataComoUtc()
        {
            var record = new LogRecord { Timestamp = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Unspecified) };

            TelemetryBsonMapper.ToDocument(record)["timestamp"].ToUniversalTime().Should().Be(new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public void Metrica_MapeaTodosLosCampos()
        {
            var record = new MetricRecord
            {
                Timestamp = Now, IntervalStart = Now.AddSeconds(-30), Service = "ApiCore", Name = "http.server.request.duration", Description = "d", Unit = "s",
                MeterName = "Microsoft.AspNetCore.Hosting", MeterVersion = "10", Kind = "Histogram", Temporality = "Delta",
                Count = 3, Sum = 6, Min = 1, Max = 3, Last = 2, Attributes = new Dictionary<String, Object?> { ["http.route"] = "/x" },
            };

            BsonDocument doc = TelemetryBsonMapper.ToDocument(record);

            doc["service"].AsString.Should().Be("ApiCore");
            doc["name"].AsString.Should().Be("http.server.request.duration");
            doc["unit"].AsString.Should().Be("s");
            doc["meter"]["name"].AsString.Should().Be("Microsoft.AspNetCore.Hosting");
            doc["meter"]["version"].AsString.Should().Be("10");
            doc["kind"].AsString.Should().Be("Histogram");
            doc["temporality"].AsString.Should().Be("Delta");
            doc["count"].AsInt64.Should().Be(3);
            doc["sum"].AsDouble.Should().Be(6);
            doc["min"].AsDouble.Should().Be(1);
            doc["max"].AsDouble.Should().Be(3);
            doc["last"].AsDouble.Should().Be(2);
            doc["intervalStart"].ToUniversalTime().Should().Be(Now.AddSeconds(-30));
            doc["attributes"]["http.route"].AsString.Should().Be("/x");
        }

        [Fact]
        public void Metrica_ConValoresNoFinitos_LosNormalizaACero()
        {
            var record = new MetricRecord { Timestamp = Now, IntervalStart = Now, Min = Double.MaxValue, Max = Double.NaN, Sum = Double.NegativeInfinity };

            BsonDocument doc = TelemetryBsonMapper.ToDocument(record);

            doc["max"].AsDouble.Should().Be(0);
            doc["sum"].AsDouble.Should().Be(0);
        }

        [Fact]
        public void Traza_MapeaCamposEventosYEnlaces()
        {
            var record = new TraceRecord
            {
                Timestamp = Now, Service = "Etl", TraceId = "t", SpanId = "s", ParentSpanId = "p", Name = "etl.job", Kind = "Internal",
                SourceName = "Connection360", SourceVersion = "1", StartTime = Now, EndTime = Now.AddSeconds(2), DurationMs = 2000,
                Status = "Error", StatusDescription = "falló", Attributes = new Dictionary<String, Object?> { ["etl.job"] = "x" },
                Events = new[] { new SpanEventRecord { Name = "exception", Timestamp = Now, Attributes = new Dictionary<String, Object?> { ["exception.type"] = "X" } } },
                Links = new[] { new SpanLinkRecord { TraceId = "lt", SpanId = "ls" } },
            };

            BsonDocument doc = TelemetryBsonMapper.ToDocument(record);

            doc["traceId"].AsString.Should().Be("t");
            doc["parentSpanId"].AsString.Should().Be("p");
            doc["source"]["name"].AsString.Should().Be("Connection360");
            doc["status"]["code"].AsString.Should().Be("Error");
            doc["status"]["description"].AsString.Should().Be("falló");
            doc["durationMs"].AsDouble.Should().Be(2000);
            doc["startTime"].ToUniversalTime().Should().Be(Now);
            doc["endTime"].ToUniversalTime().Should().Be(Now.AddSeconds(2));
            doc["events"].AsBsonArray.Should().ContainSingle();
            doc["events"][0]["name"].AsString.Should().Be("exception");
            doc["events"][0]["attributes"]["exception.type"].AsString.Should().Be("X");
            doc["links"][0]["traceId"].AsString.Should().Be("lt");
        }

        [Fact]
        public void Traza_SinPadre_GuardaNulo()
        {
            var record = new TraceRecord { Timestamp = Now, StartTime = Now, EndTime = Now, TraceId = "t", SpanId = "s" };

            TelemetryBsonMapper.ToDocument(record)["parentSpanId"].IsBsonNull.Should().BeTrue();
        }
    }
}
