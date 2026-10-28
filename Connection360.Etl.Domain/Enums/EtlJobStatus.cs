using System;

namespace Connection360.Etl.Domain.Enums
{
    /// <summary>
    /// Estados posibles de una corrida registrada en connection360write.etl_job_control (ver
    /// Documents/scriptetlJobControlSQL.sql).
    /// </summary>
    public enum EtlJobStatus
    {
        /// <summary>La corrida está en curso (o quedó a medias porque falló sin llegar a FAILED).</summary>
        Processing,

        /// <summary>La corrida terminó exitosamente (todas las páginas, si las hubo, se cargaron).</summary>
        Completed,

        /// <summary>La corrida terminó con error.</summary>
        Failed
    }

    /// <summary>Conversión explícita entre <see cref="EtlJobStatus"/> y el valor persistido en la columna status (VARCHAR).</summary>
    public static class EtlJobStatusExtensions
    {
        /// <summary>Valor exacto que se guarda en status (mismo motivo que <see cref="EtlJobNameExtensions.ToDbValue(EtlJobName)"/>).</summary>
        public static String ToDbValue(this EtlJobStatus status) => status switch
        {
            EtlJobStatus.Processing => "PROCESSING",
            EtlJobStatus.Completed => "COMPLETED",
            EtlJobStatus.Failed => "FAILED",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "EtlJobStatus sin valor de base de datos asignado.")
        };

        public static EtlJobStatus ToEtlJobStatus(this String dbValue) => dbValue switch
        {
            "PROCESSING" => EtlJobStatus.Processing,
            "COMPLETED" => EtlJobStatus.Completed,
            "FAILED" => EtlJobStatus.Failed,
            _ => throw new ArgumentOutOfRangeException(nameof(dbValue), dbValue, "status desconocido en etl_job_control.")
        };
    }
}
