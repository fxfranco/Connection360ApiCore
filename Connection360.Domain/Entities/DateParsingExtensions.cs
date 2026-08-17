using System.Globalization;
using static System.Net.Mime.MediaTypeNames;

namespace Connection360.Domain.Entities
{
    public static class DateParsingExtensions
    {
        private static readonly CultureInfo CultureEs = new("es-CO");
        //private static readonly String[] DateFormats = { "d/MM/yyyy", "dd/MM/yyyy" };
        // Arreglo con todos los formatos posibles que puede enviar la API externa
        private static readonly String[] DateFormats = new[]
        {
            // 1- Solo fecha numérica (Ej: "07/04/2024" o "7/4/2024")
            "dd/MM/yyyy",
            "d/M/yyyy",

            // 2- Con hora (Como el ejemplo "07/04/2024 00:00:00" o "7/4/2024 12:30:00 p. m.")
            "dd/MM/yyyy HH:mm:ss",
            "d/M/yyyy HH:mm:ss",
            "dd/MM/yyyy hh:mm:ss tt",
            "d/M/yyyy h:mm:ss tt",

            // 3- Con mes en texto abreviado (Ej: "2/oct/2025" o "02/oct/2025")
            "d/MMM/yyyy",
            "dd/MMM/yyyy",
            "d/MMM/yyyy HH:mm:ss",
            "dd/MMM/yyyy HH:mm:ss"
        };

        public static DateTime ToDateTimeOrMin(this String dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
                return DateTime.MinValue;

            var cleanDate = dateString.Trim();

            // 1. Intentamos con TryParseExact cubriendo la lista de formatos conocidos
            if (DateTime.TryParseExact(cleanDate, DateFormats, CultureEs, DateTimeStyles.None, out var exactDate))
            {
                return exactDate;
            }

            // 2. Respaldo general usando la cultura Invariante (por si viene en formato ISO o estándar)
            if (DateTime.TryParse(cleanDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var genericDate))
            {
                return genericDate;
            }

            return DateTime.MinValue;
        }

        public static Double ToDouble(this String doubleString)
        {
            Double.TryParse(doubleString, NumberStyles.Any, CultureInfo.InvariantCulture, out Double result);
            return result;
        }
    }
}
