using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;

namespace Connection360.Application.DTOs
{
    /// <summary>
    /// Parámetros generales de comportamiento del sistema, expuestos dentro de
    /// <see cref="MasterSettingsResponse.GeneralParameters"/>.
    /// </summary>
    public class GeneralParametersResponse
    {
        /// <summary>Habilita la actualización automática del seguimiento (tracking) de los envíos.</summary>
        public Boolean AutomaticTrackingUpdate { get; set; }

        /// <summary>Exige la carga de documentos como requisito del proceso.</summary>
        public Boolean RequireDocumentUpload { get; set; }

        /// <summary>Habilita el monitoreo público (sin autenticación) de envíos.</summary>
        public Boolean PublicMonitoring { get; set; }
    }
}
