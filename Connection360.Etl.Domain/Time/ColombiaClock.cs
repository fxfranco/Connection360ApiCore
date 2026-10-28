using System;

namespace Connection360.Etl.Domain.Time
{
    /// <summary>
    /// Reloj de la hora oficial de Colombia (UTC-5, sin horario de verano). Reemplaza a
    /// <c>DateTime.Now</c>/<c>DateTime.UtcNow</c> en las fechas que el sistema crea y asigna
    /// (incluidas las que se insertan en base de datos), para que no queden 5 horas desfasadas.
    /// No se usa para fechas que vienen de las APIs externas ni para fechas técnicas que se
    /// mantienen en UTC (expiración de JWT, cron/scheduler, campos *Utc, telemetría).
    /// </summary>
    /// <remarks>
    /// Resuelve la zona "America/Bogota" (IANA) o "SA Pacific Standard Time" (Windows); si la imagen
    /// de Docker no trae tzdata, cae a un desplazamiento fijo de -5 h, que es exacto porque Colombia
    /// no cambia de hora durante el año.
    /// </remarks>
    public static class ColombiaClock
    {
        private static readonly TimeSpan FixedOffset = TimeSpan.FromHours(-5);

        private static readonly TimeZoneInfo? Zone = ResolveZone();

        /// <summary>
        /// Hora actual de Colombia como "hora de pared" (<see cref="DateTimeKind.Unspecified"/>).
        /// Es la que debe usarse en columnas <c>timestamp</c> (sin zona) y en propiedades DateTime
        /// que se muestran o serializan tal cual.
        /// </summary>
        public static DateTime Now => FromUtc(DateTime.UtcNow);

        /// <summary>
        /// Misma hora de Colombia pero con <see cref="DateTimeKind.Utc"/> como marca. Solo para columnas
        /// <c>timestamp with time zone</c>: Npgsql rechaza valores que no sean Kind=Utc, por lo que la
        /// "hora de pared" de Colombia se guarda con esa marca para que no se vuelva a desplazar.
        /// </summary>
        public static DateTime NowAsUtcKind => DateTime.SpecifyKind(Now, DateTimeKind.Utc);

        /// <summary>
        /// Convierte un instante UTC (por ejemplo <see cref="DateTime.UtcNow"/>) a la hora de Colombia
        /// (Kind <see cref="DateTimeKind.Unspecified"/>).
        /// </summary>
        public static DateTime FromUtc(DateTime utc)
        {
            DateTime utcValue = utc.Kind == DateTimeKind.Local ? utc.ToUniversalTime() : DateTime.SpecifyKind(utc, DateTimeKind.Utc);

            DateTime converted = Zone is not null
                ? TimeZoneInfo.ConvertTimeFromUtc(utcValue, Zone)
                : utcValue + FixedOffset;

            return DateTime.SpecifyKind(converted, DateTimeKind.Unspecified);
        }

        private static TimeZoneInfo? ResolveZone()
        {
            foreach (String id in new[] { "America/Bogota", "SA Pacific Standard Time" })
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id);
                }
                catch (TimeZoneNotFoundException)
                {
                    // El identificador no existe en este sistema: se prueba con el siguiente.
                }
                catch (InvalidTimeZoneException)
                {
                    // Datos de zona horaria no válidos en este sistema: se prueba con el siguiente identificador.
                }
            }

            return null;
        }
    }
}
