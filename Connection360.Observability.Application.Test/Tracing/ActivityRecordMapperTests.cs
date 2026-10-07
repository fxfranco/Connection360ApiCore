using System.Diagnostics;
using Connection360.Observability.Application.Tracing;
using Connection360.Observability.Domain.Models;
using FluentAssertions;
using Xunit;

namespace Connection360.Observability.Application.Test.Tracing
{
    public class ActivityRecordMapperTests : IDisposable
    {
        private readonly ActivitySource _source = new("test.mapper." + Guid.NewGuid(), "9.9.9");
        private readonly ActivityListener _listener;
        private readonly ActivityRecordMapper _mapper = new(new ServiceIdentity("Etl", "2.0", "Testing", "pc:7"), 50);

        public ActivityRecordMapperTests()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == _source.Name,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public void Dispose()
        {
            _listener.Dispose();
            _source.Dispose();
        }

        [Fact]
        public void Map_CopiaIdentidadNombreYCorrelacion()
        {
            using Activity activity = _source.StartActivity("etl.job", ActivityKind.Internal)!;
            activity.Stop();

            TraceRecord record = _mapper.Map(activity);

            record.Service.Should().Be("Etl");
            record.ServiceVersion.Should().Be("2.0");
            record.Environment.Should().Be("Testing");
            record.InstanceId.Should().Be("pc:7");
            record.Name.Should().Be("etl.job");
            record.Kind.Should().Be("Internal");
            record.SourceName.Should().Be(_source.Name);
            record.SourceVersion.Should().Be("9.9.9");
            record.TraceId.Should().Be(activity.TraceId.ToHexString()).And.HaveLength(32);
            record.SpanId.Should().Be(activity.SpanId.ToHexString()).And.HaveLength(16);
            record.ParentSpanId.Should().BeNull();
        }

        [Fact]
        public void Map_ConDisplayNameMejorado_UsaElDisplayNameComoNombreDelSpan()
        {
            using Activity activity = _source.StartActivity("Microsoft.AspNetCore.Hosting.HttpRequestIn", ActivityKind.Server)!;
            activity.DisplayName = "GET /api/v1/home/totals";
            activity.Stop();

            _mapper.Map(activity).Name.Should().Be("GET /api/v1/home/totals");
        }

        [Fact]
        public void Map_ConSpanPadre_RegistraElParentSpanId()
        {
            using Activity parent = _source.StartActivity("padre")!;
            using Activity child = _source.StartActivity("hijo")!;
            child.Stop();

            _mapper.Map(child).ParentSpanId.Should().Be(parent.SpanId.ToHexString());
        }

        [Fact]
        public void Map_CalculaInicioFinYDuracion()
        {
            using Activity activity = _source.StartActivity("medido")!;
            activity.SetEndTime(activity.StartTimeUtc.AddMilliseconds(250));
            activity.Stop();

            TraceRecord record = _mapper.Map(activity);

            record.StartTime.Should().Be(activity.StartTimeUtc);
            record.Timestamp.Should().Be(activity.StartTimeUtc);
            record.DurationMs.Should().BeApproximately(250, 1);
            record.EndTime.Should().BeCloseTo(activity.StartTimeUtc.AddMilliseconds(250), TimeSpan.FromMilliseconds(1));
        }

        [Fact]
        public void Map_ConEstadoError_RegistraEstadoYDescripcion()
        {
            using Activity activity = _source.StartActivity("falla")!;
            activity.SetStatus(ActivityStatusCode.Error, "no se pudo");
            activity.Stop();

            TraceRecord record = _mapper.Map(activity);

            record.Status.Should().Be("Error");
            record.StatusDescription.Should().Be("no se pudo");
        }

        [Fact]
        public void Map_NormalizaLosTags()
        {
            using Activity activity = _source.StartActivity("tags")!;
            activity.SetTag("http.method", "GET");
            activity.SetTag("http.status_code", 200);
            activity.SetTag("largo", new String('z', 500));
            activity.SetTag("guid", Guid.Empty);
            activity.Stop();

            TraceRecord record = _mapper.Map(activity);

            record.Attributes["http.method"].Should().Be("GET");
            record.Attributes["http.status_code"].Should().Be(200);
            ((String)record.Attributes["largo"]!).Length.Should().Be(50);
            record.Attributes["guid"].Should().Be(Guid.Empty.ToString());
        }

        [Fact]
        public void Map_IncluyeEventosYEnlaces()
        {
            var linked = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);
            using Activity activity = _source.StartActivity("con eventos", ActivityKind.Internal, default(ActivityContext), links: new[] { new ActivityLink(linked) })!;
            activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection { { "exception.type", "X" } }));
            activity.Stop();

            TraceRecord record = _mapper.Map(activity);

            record.Events.Should().ContainSingle().Which.Name.Should().Be("exception");
            record.Events[0].Attributes["exception.type"].Should().Be("X");
            record.Links.Should().ContainSingle().Which.TraceId.Should().Be(linked.TraceId.ToHexString());
            record.Links[0].SpanId.Should().Be(linked.SpanId.ToHexString());
        }

        [Fact]
        public void Map_SinEventosNiEnlaces_DevuelveColeccionesVacias()
        {
            using Activity activity = _source.StartActivity("simple")!;
            activity.Stop();

            TraceRecord record = _mapper.Map(activity);

            record.Events.Should().BeEmpty();
            record.Links.Should().BeEmpty();
        }
    }
}
