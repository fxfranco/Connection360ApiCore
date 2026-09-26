namespace Connection360.Application.DTOs
{
    /// <summary>
    /// Parámetros de sistema de la configuración maestra, expuestos dentro de
    /// <see cref="MasterSettingsResponse.System"/>.
    /// </summary>
    public class SystemResponse
    {
        /// <summary>Zona horaria usada por defecto (por ejemplo, "America/Bogota").</summary>
        public String TimeZone {  get; set; } = String.Empty;

        /// <summary>Cantidad de días que se conservan los datos históricos antes de purgarse.</summary>
        public Int16 DataRetentionDays { get; set; }
    }
}
