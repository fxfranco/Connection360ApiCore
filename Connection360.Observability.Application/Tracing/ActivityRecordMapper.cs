using System.Diagnostics;
using Connection360.Observability.Domain.Models;

namespace Connection360.Observability.Application.Tracing
{
    /// <summary>Convierte un <see cref="Activity"/> de .NET en un <see cref="TraceRecord"/> con la identidad del servicio.</summary>
    public sealed class ActivityRecordMapper
    {
        private readonly ServiceIdentity _identity;
        private readonly Int32 _maxFieldLength;

        public ActivityRecordMapper(ServiceIdentity identity, Int32 maxFieldLength)
        {
            _identity = identity;
            _maxFieldLength = maxFieldLength;
        }

        public TraceRecord Map(Activity activity)
        {
            DateTime start = activity.StartTimeUtc;

            return new TraceRecord
            {
                Timestamp = start,
                Service = _identity.ServiceName,
                ServiceVersion = _identity.ServiceVersion,
                Environment = _identity.Environment,
                InstanceId = _identity.InstanceId,
                TraceId = activity.TraceId.ToHexString(),
                SpanId = activity.SpanId.ToHexString(),
                ParentSpanId = activity.ParentSpanId == default ? null : activity.ParentSpanId.ToHexString(),
                // DisplayName vale lo mismo que OperationName salvo cuando alguien lo mejora (p. ej. "GET /api/v1/home/totals" en peticiones HTTP).
                Name = TelemetryValue.Truncate(activity.DisplayName, _maxFieldLength),
                Kind = activity.Kind.ToString(),
                SourceName = activity.Source.Name,
                SourceVersion = activity.Source.Version,
                StartTime = start,
                EndTime = start + activity.Duration,
                DurationMs = activity.Duration.TotalMilliseconds,
                Status = activity.Status.ToString(),
                StatusDescription = activity.StatusDescription is null ? null : TelemetryValue.Truncate(activity.StatusDescription, _maxFieldLength),
                Attributes = ToAttributes(activity.TagObjects),
                Events = activity.Events.Select(e => new SpanEventRecord
                {
                    Name = e.Name,
                    Timestamp = e.Timestamp.UtcDateTime,
                    Attributes = ToAttributes(e.Tags),
                }).ToArray(),
                Links = activity.Links.Select(l => new SpanLinkRecord
                {
                    TraceId = l.Context.TraceId.ToHexString(),
                    SpanId = l.Context.SpanId.ToHexString(),
                    Attributes = ToAttributes(l.Tags),
                }).ToArray(),
            };
        }

        private Dictionary<String, Object?> ToAttributes(IEnumerable<KeyValuePair<String, Object?>>? tags)
        {
            var attributes = new Dictionary<String, Object?>();
            if (tags is null)
            {
                return attributes;
            }

            foreach (var tag in tags)
            {
                attributes[tag.Key] = TelemetryValue.Normalize(tag.Value, _maxFieldLength);
            }

            return attributes;
        }
    }
}
