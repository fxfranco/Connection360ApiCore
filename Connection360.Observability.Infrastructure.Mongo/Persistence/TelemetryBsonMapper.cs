using Connection360.Observability.Domain.Models;
using MongoDB.Bson;

namespace Connection360.Observability.Infrastructure.Mongo.Persistence
{
    /// <summary>
    /// Convierte los registros de dominio a documentos BSON con nombres en camelCase. Todos llevan
    /// "service" (ApiCore, ApiNotification o Etl) y "timestamp" (UTC), y los atributos se guardan
    /// como sub-documento con los nombres originales de OpenTelemetry (por ejemplo "http.request.method";
    /// los nombres con punto requieren MongoDB 5.0 o superior).
    /// </summary>
    public static class TelemetryBsonMapper
    {
        public static BsonDocument ToDocument(LogRecord record)
        {
            var document = Common(record);
            document["level"] = record.Level;
            document["severityNumber"] = record.SeverityNumber;
            document["category"] = record.Category;
            document["eventId"] = record.EventId;
            document["eventName"] = OrNull(record.EventName);
            document["message"] = record.Message;
            document["messageTemplate"] = OrNull(record.MessageTemplate);
            document["attributes"] = Attributes(record.Attributes);
            document["exception"] = record.ExceptionType is null
                ? BsonNull.Value
                : new BsonDocument
                {
                    { "type", record.ExceptionType },
                    { "message", OrNull(record.ExceptionMessage) },
                    { "stackTrace", OrNull(record.ExceptionStackTrace) },
                };
            document["traceId"] = OrNull(record.TraceId);
            document["spanId"] = OrNull(record.SpanId);
            return document;
        }

        public static BsonDocument ToDocument(MetricRecord record)
        {
            var document = Common(record);
            document["intervalStart"] = Date(record.IntervalStart);
            document["name"] = record.Name;
            document["description"] = OrNull(record.Description);
            document["unit"] = OrNull(record.Unit);
            document["meter"] = new BsonDocument { { "name", record.MeterName }, { "version", OrNull(record.MeterVersion) } };
            document["kind"] = record.Kind;
            document["temporality"] = record.Temporality;
            document["count"] = record.Count;
            document["sum"] = Finite(record.Sum);
            document["min"] = Finite(record.Min);
            document["max"] = Finite(record.Max);
            document["last"] = Finite(record.Last);
            document["attributes"] = Attributes(record.Attributes);
            return document;
        }

        public static BsonDocument ToDocument(TraceRecord record)
        {
            var document = Common(record);
            document["traceId"] = record.TraceId;
            document["spanId"] = record.SpanId;
            document["parentSpanId"] = OrNull(record.ParentSpanId);
            document["name"] = record.Name;
            document["kind"] = record.Kind;
            document["source"] = new BsonDocument { { "name", record.SourceName }, { "version", OrNull(record.SourceVersion) } };
            document["startTime"] = Date(record.StartTime);
            document["endTime"] = Date(record.EndTime);
            document["durationMs"] = Finite(record.DurationMs);
            document["status"] = new BsonDocument { { "code", record.Status }, { "description", OrNull(record.StatusDescription) } };
            document["attributes"] = Attributes(record.Attributes);
            document["events"] = new BsonArray(record.Events.Select(e => new BsonDocument
            {
                { "name", e.Name },
                { "timestamp", Date(e.Timestamp) },
                { "attributes", Attributes(e.Attributes) },
            }));
            document["links"] = new BsonArray(record.Links.Select(l => new BsonDocument
            {
                { "traceId", l.TraceId },
                { "spanId", l.SpanId },
                { "attributes", Attributes(l.Attributes) },
            }));
            return document;
        }

        private static BsonDocument Common(TelemetryRecord record) => new()
        {
            { "timestamp", Date(record.Timestamp) },
            { "service", record.Service },
            { "serviceVersion", record.ServiceVersion },
            { "environment", record.Environment },
            { "instanceId", record.InstanceId },
        };

        public static BsonDocument Attributes(IReadOnlyDictionary<String, Object?> attributes)
        {
            var document = new BsonDocument();
            foreach (var pair in attributes)
            {
                // MongoDB no admite campos que empiecen con "$".
                String key = pair.Key.StartsWith('$') ? "_" + pair.Key[1..] : pair.Key;
                if (key.Length == 0)
                {
                    continue;
                }

                document[key] = ToBson(pair.Value);
            }

            return document;
        }

        private static BsonValue ToBson(Object? value) => value switch
        {
            null => BsonNull.Value,
            String text => text,
            Boolean flag => flag,
            Int32 int32 => int32,
            Int64 int64 => int64,
            Double dbl => Finite(dbl),
            Single sgl => Finite(sgl),
            Byte or SByte or Int16 or UInt16 => Convert.ToInt32(value),
            DateTime date => Date(date),
            IEnumerable<Object?> items => new BsonArray(items.Select(ToBson)),
            _ => value.ToString() ?? String.Empty,
        };

        private static BsonValue OrNull(String? text) => text is null ? BsonNull.Value : text;

        private static BsonValue Date(DateTime value)
            => new BsonDateTime(value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime());

        /// <summary>BSON admite NaN/Infinito, pero muchas plataformas de análisis no: se normalizan a 0.</summary>
        private static BsonValue Finite(Double value) => Double.IsFinite(value) ? value : 0d;
    }
}
