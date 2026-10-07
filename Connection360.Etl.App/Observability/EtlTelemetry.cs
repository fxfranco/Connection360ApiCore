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
    }
}
