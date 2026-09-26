using System.Text.Json.Serialization;

namespace Connection360.Application.DTOs
{
    /// <summary>
    /// Configuración maestra (global) del sistema, devuelta por
    /// <c>SettingsController.GetMasterSettings</c>.
    /// </summary>
    public class MasterSettingsResponse
    {
        /// <summary>Identificador del registro de configuración maestra.</summary>
        public Int64 IdMasterSettings {  get; set; }

        /// <summary>Parámetros generales de comportamiento. Se omite del JSON cuando es <c>null</c>.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GeneralParametersResponse? GeneralParameters {  get; set; }

        /// <summary>Parámetros de configuración regional. Se omite del JSON cuando es <c>null</c>.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public LocationResponse? Location {  get; set; }

        /// <summary>Parámetros de sistema (zona horaria, retención de datos). Se omite del JSON cuando es <c>null</c>.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SystemResponse? System {  get; set; }
    }
}
