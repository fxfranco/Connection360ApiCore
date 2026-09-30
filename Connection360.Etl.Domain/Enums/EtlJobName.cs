using System;

namespace Connection360.Etl.Domain.Enums
{
    /// <summary>
    /// Procesos ETL controlados a través de connection360write.etl_job_control (ver
    /// Documents/scriptetlJobControlSQL.sql). Cada valor corresponde a un "job_name" propio.
    /// </summary>
    public enum EtlJobName
    {
        /// <summary>Proceso ETL principal: bodega de datos de envíos (RunEtlProcessUseCase).</summary>
        ApplicationDataSheet,

        /// <summary>Proceso ETL de logs: histórico de cambios de estado (RunLogsEtlProcessUseCase).</summary>
        LogStatusTracking,

        /// <summary>
        /// Marcador de control (no es un proceso propio): mientras no exista un registro COMPLETED
        /// de este job, RunEtlProcessUseCase trata la corrida como la migración/carga inicial de
        /// connection360write.application_data_sheet y omite la detección de cambios (estado/
        /// comentario). Una vez completa sin error, queda marcado COMPLETED para siempre: las
        /// corridas siguientes ya hacen la validación completa de cambios.
        /// </summary>
        ApplicationDataSheetMigration
    }

    /// <summary>Conversión explícita entre <see cref="EtlJobName"/> y el valor persistido en la columna job_name (VARCHAR).</summary>
    public static class EtlJobNameExtensions
    {
        /// <summary>
        /// Valor exacto que se guarda en job_name. Se convierte manualmente (en vez de dejar que
        /// Dapper serialice el enum) porque, sin un type handler, Dapper envía el entero subyacente
        /// del enum como parámetro, no su nombre - y aquí la columna es un VARCHAR legible.
        /// </summary>
        public static String ToDbValue(this EtlJobName jobName) => jobName switch
        {
            EtlJobName.ApplicationDataSheet => "application_data_sheet",
            EtlJobName.LogStatusTracking => "log_status_tracking",
            EtlJobName.ApplicationDataSheetMigration => "application_data_sheet_migration",
            _ => throw new ArgumentOutOfRangeException(nameof(jobName), jobName, "EtlJobName sin valor de base de datos asignado.")
        };

        public static EtlJobName ToEtlJobName(this String dbValue) => dbValue switch
        {
            "application_data_sheet" => EtlJobName.ApplicationDataSheet,
            "log_status_tracking" => EtlJobName.LogStatusTracking,
            "application_data_sheet_migration" => EtlJobName.ApplicationDataSheetMigration,
            _ => throw new ArgumentOutOfRangeException(nameof(dbValue), dbValue, "job_name desconocido en etl_job_control.")
        };
    }
}
