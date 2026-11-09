using System.Diagnostics;
using System.Diagnostics.Metrics;
using Connection360.Etl.Application.DTOs;
using Connection360.Observability.Domain.Telemetry;

namespace Connection360.Etl.App.Observability
{
    /// <summary>
    /// Instrumentación propia del proceso ETL con las librerías nativas de .NET (ActivitySource y
    /// Meter de <see cref="Connection360Telemetry"/>): un span por corrida y métricas de corridas,
    /// duración y registros extraídos/transformados/cargados. Si la observabilidad está desactivada,
    /// el span es null y las métricas no tienen ningún listener: no hace nada.
    /// </summary>
    internal static class EtlTelemetry
    {
        public const String LogsJob = "logs";
        public const String MainJob = "application_data_sheet";
        public const String ViewsJob = "materialized_views";

        private static readonly Counter<long> Runs = Connection360Telemetry.Meter.CreateCounter<long>(
            "etl.runs", unit: "{run}", description: "Corridas del ETL, por tarea y resultado.");

        private static readonly Histogram<double> RunDuration = Connection360Telemetry.Meter.CreateHistogram<double>(
            "etl.run.duration", unit: "s", description: "Duración de cada corrida del ETL.");

        private static readonly Counter<long> Extracted = Connection360Telemetry.Meter.CreateCounter<long>(
            "etl.records.extracted", unit: "{record}", description: "Registros extraídos de las APIs externas.");

        private static readonly Counter<long> Transformed = Connection360Telemetry.Meter.CreateCounter<long>(
            "etl.records.transformed", unit: "{record}", description: "Registros transformados.");

        private static readonly Counter<long> Loaded = Connection360Telemetry.Meter.CreateCounter<long>(
            "etl.records.loaded", unit: "{record}", description: "Registros cargados en la base de datos.");

        private static readonly Counter<long> ViewRefreshes = Connection360Telemetry.Meter.CreateCounter<long>(
            "etl.views.refreshes", unit: "{refresh}", description: "Refrescos de vistas materializadas, por vista y resultado.");

        private static readonly Histogram<double> ViewRefreshDuration = Connection360Telemetry.Meter.CreateHistogram<double>(
            "etl.views.refresh.duration", unit: "s", description: "Duración del refresco de cada vista materializada.");

        /// <summary>Inicia el span de una tarea del ETL (null si nadie está escuchando).</summary>
        public static Activity? StartJob(String job)
            => Connection360Telemetry.Source.StartActivity($"etl.{job}.run", ActivityKind.Internal)?.SetTag("etl.job", job);

        /// <summary>Registra el resultado de la tarea en su span y en las métricas.</summary>
        public static void RecordResult(Activity? span, String job, EtlRunResult result)
        {
            var jobTag = new KeyValuePair<String, Object?>("etl.job", job);

            Runs.Add(1, jobTag, new KeyValuePair<String, Object?>("success", result.Success));
            RunDuration.Record(result.Duration.TotalSeconds, jobTag);
            Extracted.Add(result.ExtractedRecordsByApi.Values.Sum(), jobTag);
            Transformed.Add(result.TransformedRecords, jobTag);
            Loaded.Add(result.LoadedRecords, jobTag);

            if (span is null)
            {
                return;
            }

            span.SetTag("etl.success", result.Success);
            span.SetTag("etl.records.extracted", result.ExtractedRecordsByApi.Values.Sum());
            span.SetTag("etl.records.transformed", result.TransformedRecords);
            span.SetTag("etl.records.loaded", result.LoadedRecords);
            span.SetStatus(result.Success ? ActivityStatusCode.Ok : ActivityStatusCode.Error, result.Success ? null : result.ErrorMessage);
        }

        /// <summary>Registra el resultado de la sincronización de vistas materializadas en su span y en las métricas.</summary>
        public static void RecordViewsResult(Activity? span, MaterializedViewsRefreshResult result)
        {
            foreach (var outcome in result.Views)
            {
                var viewTag = new KeyValuePair<String, Object?>("etl.view", outcome.View.ToString());
                ViewRefreshes.Add(1, viewTag, new KeyValuePair<String, Object?>("success", outcome.Success));
                ViewRefreshDuration.Record(outcome.Duration.TotalSeconds, viewTag);
            }

            if (span is null)
            {
                return;
            }

            span.SetTag("etl.success", result.Success);
            span.SetTag("etl.views.skipped", result.Skipped);
            span.SetTag("etl.views.refreshed", result.Views.Count(v => v.Success));
            span.SetTag("etl.views.failed", result.Views.Count(v => !v.Success));
            span.SetStatus(result.Success ? ActivityStatusCode.Ok : ActivityStatusCode.Error,
                result.Success ? null : String.Join("; ", result.Views.Where(v => !v.Success).Select(v => $"{v.View}: {v.ErrorMessage}")));
        }
    }
}
