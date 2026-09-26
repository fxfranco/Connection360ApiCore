using System;
using System.Collections.Generic;
using System.Text;

namespace Connection360.Application.DTOs
{
    /// <summary>
    /// Parámetros de configuración regional del sistema, expuestos dentro de
    /// <see cref="MasterSettingsResponse.Location"/>.
    /// </summary>
    public class LocationResponse
    {
        /// <summary>Tipo/código de moneda usado por defecto (por ejemplo, "USD", "COP").</summary>
        public String CurrencyType { get; set; } = String.Empty;

        /// <summary>Idioma por defecto de la aplicación (por ejemplo, "es", "en").</summary>
        public String Language { get; set; } = String.Empty;
    }
}
