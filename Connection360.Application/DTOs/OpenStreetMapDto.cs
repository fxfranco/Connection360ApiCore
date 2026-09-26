using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Connection360.Application.DTOs
{
    /// <summary>
    /// Resultado de geocodificación de un lugar (nombre + coordenadas) obtenido a través del
    /// servicio externo de OpenStreetMap/Nominatim, usado para resolver las coordenadas de origen y
    /// destino de un envío (ver <see cref="TrackingShipmentsResponse"/>).
    /// </summary>
    public class OpenStreetMapDto
    {
        /// <summary>Nombre del lugar tal como lo devuelve el proveedor de geocodificación.</summary>
        public String PlaceName { get; set; } = String.Empty;

        /// <summary>Latitud del lugar.</summary>
        public String Latitud { get; set; } = String.Empty;

        /// <summary>Longitud del lugar.</summary>
        public String Longitud { get; set; } = String.Empty;
    }
}
