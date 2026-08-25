using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Connection360.Application.DTOs
{
    public class MasterSettingsResponse
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GeneralParametersResponse? GeneralParameters {  get; set; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public LocationResponse? Location {  get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SystemResponse? System {  get; set; }
    }
}
