namespace Connection360.Application.DTOs.Persistence
{
    public record CreateMasterSettingsDto(Boolean AutomaticTrackingUpdate, Boolean RequireDocumentUpload, Boolean PublicMonitoring, String CurrencyType, String Language, String TimeZone, Int16 DataRetentionDays);
}
