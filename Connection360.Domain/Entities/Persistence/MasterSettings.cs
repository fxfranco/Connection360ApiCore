namespace Connection360.Domain.Entities.Persistence
{
    public class MasterSettings
    {
        public Int64 IdMasterSettings { get; private set; }
        public Boolean AutomaticTrackingUpdate { get; private set; }
        public Boolean RequireDocumentUpload { get; private set; }
        public Boolean PublicMonitoring { get; private set; }
        public String? CurrencyType { get; private set; }
        public String? Language { get; private set; }
        public String? TimeZone { get; private set; }
        public Int16 DataRetentionDays { get; private set; }

        public MasterSettings()
        {
        }

        public MasterSettings(Int64 idMasterSettings, Boolean automaticTrackingUpdate, Boolean requireDocumentUpload, Boolean publicMonitoring, String currencyType, String language, String timeZone, Int16 dataRetentionDays)
        {
            IdMasterSettings = idMasterSettings;
            Update(automaticTrackingUpdate, requireDocumentUpload, publicMonitoring, currencyType, language , timeZone, dataRetentionDays);
        }

        public void Update(Boolean automaticTrackingUpdate, Boolean requireDocumentUpload, Boolean publicMonitoring, String currencyType, String language, String timeZone, Int16 dataRetentionDays)
        {
            AutomaticTrackingUpdate = automaticTrackingUpdate;
            RequireDocumentUpload = requireDocumentUpload;
            PublicMonitoring = publicMonitoring;
            CurrencyType = currencyType;
            Language = language;
            TimeZone = timeZone;
            DataRetentionDays = dataRetentionDays;
        }
    }
}
