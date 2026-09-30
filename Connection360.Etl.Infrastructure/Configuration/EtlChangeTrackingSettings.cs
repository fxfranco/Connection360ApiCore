using System;

namespace Connection360.Etl.Infrastructure.Configuration
{
    /// <summary>
    /// Configuración global (appsettings, sección "EtlChangeTracking") de la detección de cambios de
    /// ESTADO/COMENTARIO/FECHA COMENTARIO en el proceso ETL principal.
    /// </summary>
    public class EtlChangeTrackingSettings
    {
        /// <summary>Nombre de la sección donde está la configuración.</summary>
        public const String SectionName = "EtlChangeTracking";

        /// <summary>
        /// Usuario "de sistema" que queda registrado como autor de cada cambio insertado en
        /// connection360write.log_status_tracking.usuario_cambio: el proceso ETL, no una persona,
        /// es quien detecta y registra el cambio.
        /// </summary>
        public String SystemUser { get; set; } = "ETL_CONNECTION360";
    }
}
