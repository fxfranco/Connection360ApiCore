using System;

namespace Connection360.Etl.Infrastructure.Configuration
{
    /// <summary>
    /// Configuración global (appsettings, sección "EtlJobControl") del proceso de depuración de
    /// connection360write.etl_job_control (ver PurgeEtlJobControlUseCase). Solo aplica al job
    /// "application_data_sheet": los registros de "log_status_tracking" nunca se depuran.
    /// </summary>
    public class EtlJobControlSettings
    {
        /// <summary>Nombre de la sección donde está la configuración.</summary>
        public const String SectionName = "EtlJobControl";

        /// <summary>
        /// Cantidad de días (con base en updated_at) a partir de la cual un registro de
        /// application_data_sheet en etl_job_control se considera candidato a depuración.
        /// </summary>
        public Int32 ApplicationDataSheetRetentionDays { get; set; } = 30;
    }
}
