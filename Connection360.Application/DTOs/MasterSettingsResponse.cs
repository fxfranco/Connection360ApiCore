using System.Text.Json.Serialization;

namespace Connection360.Application.DTOs
{
    public class MasterSettingsResponse
    {
        public Int64 IdMasterSettings {  get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GeneralParametersResponse? GeneralParameters {  get; set; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public LocationResponse? Location {  get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SystemResponse? System {  get; set; }
    }
}
