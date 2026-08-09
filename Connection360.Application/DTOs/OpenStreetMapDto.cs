using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Connection360.Application.DTOs
{
    public class OpenStreetMapDto
    {
        public String PlaceName { get; set; } = String.Empty;
        public String Latitud { get; set; } = String.Empty;
        public String Longitud { get; set; } = String.Empty;
    }
}
