namespace Connection360.Application.DTOs.Persistence
{
    public record MasterSettingsResponseDto(Int64 IdMasterSettings, Boolean AutomaticTrackingUpdate, Boolean RequireDocumentUpload, Boolean PublicMonitoring, String CurrencyType, String Language, String TimeZone, Int16 DataRetentionDays);
}
