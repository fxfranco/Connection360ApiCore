using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;

namespace Connection360.Application.DTOs
{
    public class GeneralParametersResponse
    {
        public Boolean AutomaticTrackingUpdate { get; set; }
        public Boolean RequireDocumentUpload { get; set; }
        public Boolean PublicMonitoring { get; set; }
    }
}
