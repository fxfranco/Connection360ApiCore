using System;
using System.Collections.Generic;

namespace Connection360.Etl.Application.DTOs
{
    /// <summary>
    /// Resultado de una ejecución completa del proceso ETL (Extract -> Transform -> Load),
    /// pensado para quedar en el log/consola de Connection360.Etl.App y, a futuro, poder
    /// persistirse como una bitácora de corridas.
    /// </summary>
    public class EtlRunResult
    {
        /// <summary>Momento (UTC) en que inició la ejecución.</summary>
        public DateTime StartedAtUtc { get; set; }

        /// <summary>Momento (UTC) en que finalizó la ejecución (exitosa o no).</summary>
        public DateTime FinishedAtUtc { get; set; }

        /// <summary>Cantidad de registros obtenidos por API externa consultada (paso Extract).</summary>
        public Dictionary<String, Int32> ExtractedRecordsByApi { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Cantidad de filas producidas por el paso Transform (una por documento de transporte).</summary>
        public Int32 TransformedRecords { get; set; }

        /// <summary>Cantidad de filas insertadas/actualizadas en la bodega de datos (paso Load).</summary>
        public Int32 LoadedRecords { get; set; }

        /// <summary>
        /// true cuando esta corrida fue la migración/carga inicial de application_data_sheet (no
        /// existía todavía un registro COMPLETED del job "application_data_sheet_migration" en
        /// etl_job_control): en ese caso no se detectan cambios en absoluto, así que
        /// <see cref="StateChangesDetected"/> y <see cref="CommentChangesDetected"/> quedan en 0.
        /// </summary>
        public Boolean IsInitialMigrationRun { get; set; }

        /// <summary>
        /// Cantidad de documentos en los que se detectó un cambio de ESTADO frente a lo que ya había
        /// en application_data_sheet (cada uno generó una fila en log_status_tracking y un mensaje en
        /// outbox_messages con event_type "ChangeState").
        /// </summary>
        public Int32 StateChangesDetected { get; set; }

        /// <summary>
        /// Cantidad de documentos en los que se detectó un cambio de COMENTARIO y/o FECHA COMENTARIO
        /// (cada uno generó un mensaje en outbox_messages con event_type "Comment").
        /// </summary>
        public Int32 CommentChangesDetected { get; set; }

        /// <summary>Indica si la ejecución terminó sin errores.</summary>
        public Boolean Success { get; set; }

        /// <summary>Mensaje de error, si <see cref="Success"/> es falso.</summary>
        public String? ErrorMessage { get; set; }

        /// <summary>Duración total de la ejecución.</summary>
        public TimeSpan Duration => FinishedAtUtc - StartedAtUtc;
    }
}
