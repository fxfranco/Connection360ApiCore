using System;
using System.Collections.Generic;
using System.Text;

namespace Connection360.Application.DTOs
{
    public class NotificationChannelsResponse
    {
        public Boolean Application {  get; set; }
        public Boolean Email {  get; set; }
        public Boolean TextMessages {  get; set; }
    }
}
