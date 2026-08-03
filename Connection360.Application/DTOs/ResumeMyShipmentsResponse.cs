using System;
using System.Collections.Generic;
using System.Text;

namespace Connection360.Application.DTOs
{
    public class ResumeMyShipmentsResponse
    {
        public Int64 Id { get; set; }
        public String ShipmentMode { get; set; } = String.Empty;
        public String DocumentNumber { get; set; } = String.Empty;
        public String State { get; set; } = String.Empty;
        public String OperationType { get; set; } = String.Empty;
        public String ClientName { get; set; } = String.Empty;
        public String Origin { get; set; } = String.Empty;
        public String Destination { get; set; } = String.Empty;
        public DateTime ETDDate { get; set; }
        public DateTime ATDDate { get; set; }
        public DateTime ETADate { get; set; }
        public DateTime ATADate { get; set; }
    }
}
