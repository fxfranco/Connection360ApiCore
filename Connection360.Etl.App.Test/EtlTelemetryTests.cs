using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Connection360.Etl.App.Observability;
using Connection360.Etl.Application.DTOs;
using Connection360.Observability.Domain.Telemetry;
using FluentAssertions;
using Xunit;

namespace Connection360.Etl.App.Test
{
    /// <summary>Instrumentación del ETL: span por tarea y métricas etl.* (ver EtlTelemetry).</summary>
    [Collection("TelemetriaEtl")]
    public class EtlTelemetryTests : IDisposable
    {
        private readonly ActivityListener _activityListener;
        private readonly MeterListener _meterListener;
        private readonly ConcurrentQueue<(String Name, Double Value, Dictionary<String, Object?> Tags)> _measurements = new();
        private Boolean _listening = true;

        public EtlTelemetryTests()
        {
            _activityListener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == Connection360Telemetry.Name,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => _listening ? ActivitySamplingResult.AllDataAndRecorded : ActivitySamplingResult.None,
            };
            ActivitySource.AddActivityListener(_activityListener);

            _meterListener = new MeterListener
            {
                InstrumentPublished = (instrument, listener) =>
                {
                    if (instrument.Meter.Name == Connection360Telemetry.Name && instrument.Name.StartsWith("etl.", StringComparison.Ordinal))
                        listener.EnableMeasurementEvents(instrument);
                },
            };
            _meterListener.SetMeasurementEventCallback<Int64>((i, v, tags, _) => _measurements.Enqueue((i.Name, v, ToDictionary(tags))));
            _meterListener.SetMeasurementEventCallback<Double>((i, v, tags, _) => _measurements.Enqueue((i.Name, v, ToDictionary(tags))));
            _meterListener.Start();
        }

        public void Dispose()
        {
            _activityListener.Dispose();
            _meterListener.Dispose();
        }

        private static Dictionary<String, Object?> ToDictionary(ReadOnlySpan<KeyValuePair<String, Object?>> tags)
        {
            var result = new Dictionary<String, Object?>();
            foreach (var tag in tags) result[tag.Key] = tag.Value;
            return result;
        }

        private static EtlRunResult Result(Boolean success, Int32 transformed, Int32 loaded, String? error = null, params (String Api, Int32 Count)[] extracted)
        {
            var result = new EtlRunResult
            {
                Success = success, ErrorMessage = error, TransformedRecords = transformed, LoadedRecords = loaded,
                StartedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), FinishedAtUtc = new DateTime(2026, 1, 1, 0, 0, 4, DateTimeKind.Utc),
            };
            foreach (var (api, count) in extracted) result.ExtractedRecordsByApi[api] = count;
            return result;
        }

        [Fact]
        public void StartJob_ConListener_CreaElSpanConNombreYEtiquetaDeLaTarea()
        {
            using Activity? span = EtlTelemetry.StartJob(EtlTelemetry.LogsJob);

            span.Should().NotBeNull();
            span!.OperationName.Should().Be("etl.logs.run");
            span.Kind.Should().Be(ActivityKind.Internal);
            span.GetTagItem("etl.job").Should().Be("logs");
        }

        [Fact]
        public void StartJob_SinListener_DevuelveNulo()
        {
            _listening = false;

            Activity? span = EtlTelemetry.StartJob(EtlTelemetry.MainJob);

            span.Should().BeNull();
        }

        [Fact]
        public void RecordResult_Exitoso_AnotaElSpanComoOkConLosConteos()
        {
            using Activity span = EtlTelemetry.StartJob(EtlTelemetry.MainJob)!;

            EtlTelemetry.RecordResult(span, EtlTelemetry.MainJob, Result(true, 5, 4, null, ("BPMS", 5), ("SIM", 2)));

            span.Status.Should().Be(ActivityStatusCode.Ok);
            span.GetTagItem("etl.success").Should().Be(true);
            span.GetTagItem("etl.records.extracted").Should().Be(7);
            span.GetTagItem("etl.records.transformed").Should().Be(5);
            span.GetTagItem("etl.records.loaded").Should().Be(4);
        }

        [Fact]
        public void RecordResult_Fallido_MarcaElSpanEnErrorConElMensaje()
        {
            using Activity span = EtlTelemetry.StartJob(EtlTelemetry.LogsJob)!;

            EtlTelemetry.RecordResult(span, EtlTelemetry.LogsJob, Result(false, 0, 0, "sin conexión"));

            span.Status.Should().Be(ActivityStatusCode.Error);
            span.StatusDescription.Should().Be("sin conexión");
            span.GetTagItem("etl.success").Should().Be(false);
        }

        [Fact]
        public void RecordResult_RegistraLasMetricasConLaEtiquetaDeLaTarea()
        {
            EtlTelemetry.RecordResult(null, EtlTelemetry.MainJob, Result(true, 5, 4, null, ("BPMS", 6)));

            _measurements.Should().Contain(m => m.Name == "etl.runs" && m.Value == 1 && (String)m.Tags["etl.job"]! == "application_data_sheet" && (Boolean)m.Tags["success"]! == true);
            _measurements.Should().Contain(m => m.Name == "etl.run.duration" && m.Value == 4);
            _measurements.Should().Contain(m => m.Name == "etl.records.extracted" && m.Value == 6);
            _measurements.Should().Contain(m => m.Name == "etl.records.transformed" && m.Value == 5);
            _measurements.Should().Contain(m => m.Name == "etl.records.loaded" && m.Value == 4);
        }

        [Fact]
        public void RecordResult_SinSpan_NoLanzaYAunAsiRegistraMetricas()
        {
            Action act = () => EtlTelemetry.RecordResult(null, EtlTelemetry.LogsJob, Result(false, 0, 0, "x"));

            act.Should().NotThrow();
            _measurements.Should().Contain(m => m.Name == "etl.runs" && (Boolean)m.Tags["success"]! == false);
        }
    }
}
