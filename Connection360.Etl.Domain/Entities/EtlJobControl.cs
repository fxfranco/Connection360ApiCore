using Connection360.Etl.Domain.Enums;
using System;

namespace Connection360.Etl.Domain.Entities
{
    /// <summary>
    /// Fila de control de ejecución de un proceso ETL, destinada a
    /// connection360write.etl_job_control (ver Documents/scriptetlJobControlSQL.sql). Permite saber
    /// si un proceso ya se ejecutó (y con qué resultado) y, si manejaba paginación, en qué página
    /// quedó antes de fallar.
    /// </summary>
    public sealed class EtlJobControl
    {
        public Int64 Id { get; set; }

        /// <summary>Proceso ETL al que corresponde esta corrida.</summary>
        public EtlJobName JobName { get; set; }

        /// <summary>Estado actual de la corrida.</summary>
        public EtlJobStatus Status { get; set; }

        /// <summary>Última página completada con éxito (null si el proceso no maneja paginación).</summary>
        public Int32? LastProcessedPage { get; set; }

        /// <summary>Tamaño de página usado durante toda la corrida (null si no maneja paginación).</summary>
        public Int32? PageSize { get; set; }

        /// <summary>Total de registros procesados/cargados hasta el momento (se va acumulando página a página).</summary>
        public Int32? TotalRecordsProcessed { get; set; }

        /// <summary>Momento (UTC) de la última actualización de este registro.</summary>
        public DateTime UpdatedAt { get; set; }
    }
}
