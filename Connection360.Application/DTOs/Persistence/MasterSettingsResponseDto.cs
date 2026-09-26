namespace Connection360.Application.DTOs.Persistence
{
    /// <summary>
    /// Configuración maestra (global) del sistema, ya persistida. Cuerpo de la solicitud/respuesta
    /// de <c>SettingsController.UpdateMasterSettings</c> y respuesta de <c>CreateMasterSettings</c>.
    /// </summary>
    /// <param name="IdMasterSettings">Identificador del registro de configuración maestra.</param>
    /// <param name="AutomaticTrackingUpdate">Habilita la actualización automática del seguimiento (tracking) de los envíos.</param>
    /// <param name="RequireDocumentUpload">Exige la carga de documentos como requisito del proceso.</param>
    /// <param name="PublicMonitoring">Habilita el monitoreo público (sin autenticación) de envíos.</param>
    /// <param name="CurrencyType">Tipo/código de moneda usado por defecto (por ejemplo, "USD", "COP").</param>
    /// <param name="Language">Idioma por defecto de la aplicación (por ejemplo, "es", "en").</param>
    /// <param name="TimeZone">Zona horaria usada por defecto (por ejemplo, "America/Bogota").</param>
    /// <param name="DataRetentionDays">Cantidad de días que se conservan los datos históricos antes de purgarse.</param>
    public record MasterSettingsResponseDto(Int64 IdMasterSettings, Boolean AutomaticTrackingUpdate, Boolean RequireDocumentUpload, Boolean PublicMonitoring, String CurrencyType, String Language, String TimeZone, Int16 DataRetentionDays);
}
