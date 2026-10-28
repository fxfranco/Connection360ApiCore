using System;
using System.Globalization;

namespace Connection360.Etl.Domain.Entities
{
    /// <summary>
    /// Extensiones de parseo de valores planos (String) provenientes de las APIs externas hacia
    /// tipos .NET. Copiado de Connection360.Domain.Entities.DateParsingExtensions (mismos formatos
    /// de fecha soportados) y ampliado con ToInt32OrDefault/ToDecimalOrDefault, necesarios para
    /// convertir los campos numéricos de <see cref="DynamicRecord"/> (todos String) a los tipos
    /// INTEGER/NUMERIC de la tabla de destino connection360write.application_data_sheet.
    /// </summary>
    public static class DateParsingExtensions
    {
        private static readonly CultureInfo CultureEs = new("es-CO");

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

        /// <summary>Igual que <see cref="ToDateTimeOrMin"/> pero devuelve <c>null</c> en vez de <see cref="DateTime.MinValue"/> cuando no hay valor, para columnas DATE NULL.</summary>
        public static DateTime? ToNullableDate(this String dateString)
        {
            var parsed = dateString.ToDateTimeOrMin();
            return parsed == DateTime.MinValue ? null : parsed;
        }

        public static Double ToDouble(this String doubleString)
        {
            Double.TryParse(doubleString, NumberStyles.Any, CultureInfo.InvariantCulture, out Double result);
            return result;
        }

        /// <summary>Convierte a Decimal (tipo usado por las columnas NUMERIC de la sábana de datos), devolviendo 0 si no se puede interpretar.</summary>
        public static Decimal ToDecimalOrDefault(this String decimalString)
        {
            Decimal.TryParse(decimalString, NumberStyles.Any, CultureInfo.InvariantCulture, out Decimal result);
            return result;
        }

        /// <summary>Convierte a Int32 (tipo usado por las columnas INTEGER de la sábana de datos), devolviendo 0 si no se puede interpretar.</summary>
        public static Int32 ToInt32OrDefault(this String intString)
        {
            Int32.TryParse(intString, NumberStyles.Any, CultureInfo.InvariantCulture, out Int32 result);
            return result;
        }

        /// <summary>Convierte a Int64 (tipo usado por columnas BIGINT, como log_status_tracking.id_operacion), devolviendo 0 si no se puede interpretar.</summary>
        public static Int64 ToInt64OrDefault(this String longString)
        {
            Int64.TryParse(longString, NumberStyles.Any, CultureInfo.InvariantCulture, out Int64 result);
            return result;
        }
    }
}
