namespace Connection360.Application.DTOs.Persistence
{
    /// <summary>
    /// Datos para crear la configuración maestra (global) del sistema. Cuerpo de la solicitud de
    /// <c>SettingsController.CreateMasterSettings</c>.
    /// </summary>
    /// <param name="AutomaticTrackingUpdate">Habilita la actualización automática del seguimiento (tracking) de los envíos.</param>
    /// <param name="RequireDocumentUpload">Exige la carga de documentos como requisito del proceso.</param>
    /// <param name="PublicMonitoring">Habilita el monitoreo público (sin autenticación) de envíos.</param>
    /// <param name="CurrencyType">Tipo/código de moneda usado por defecto (por ejemplo, "USD", "COP").</param>
    /// <param name="Language">Idioma por defecto de la aplicación (por ejemplo, "es", "en").</param>
    /// <param name="TimeZone">Zona horaria usada por defecto (por ejemplo, "America/Bogota").</param>
    /// <param name="DataRetentionDays">Cantidad de días que se conservan los datos históricos antes de purgarse.</param>
    public record CreateMasterSettingsDto(Boolean AutomaticTrackingUpdate, Boolean RequireDocumentUpload, Boolean PublicMonitoring, String CurrencyType, String Language, String TimeZone, Int16 DataRetentionDays);
}
