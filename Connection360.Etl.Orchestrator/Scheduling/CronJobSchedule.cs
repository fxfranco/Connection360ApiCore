namespace Connection360.Etl.Orchestrator.Scheduling
{
    /// <summary>
    /// Une una <see cref="CronExpression"/> ya parseada con la zona horaria en la que se deben
    /// evaluar sus campos: la hora "cron" siempre es hora LOCAL de esa zona, nunca UTC, para que
    /// por ejemplo "0 8 * * *" siga significando "todos los días a las 8am hora de Bogotá" sin
    /// importar en qué zona horaria esté configurado el servidor donde corre el orquestador.
    /// </summary>
    public sealed class CronJobSchedule
    {
        private readonly CronExpression _expression;

        public TimeZoneInfo TimeZone { get; }

        public CronJobSchedule(CronExpression expression, TimeZoneInfo timeZone)
        {
            _expression = expression;
            TimeZone = timeZone;
        }

        /// <summary>Próxima ejecución, en UTC, estrictamente posterior a <paramref name="afterUtc"/>.</summary>
        public DateTimeOffset GetNextOccurrenceUtc(DateTimeOffset afterUtc)
        {
            DateTime afterLocal = TimeZoneInfo.ConvertTime(afterUtc, TimeZone).DateTime;
            DateTime nextLocal = _expression.GetNextOccurrence(afterLocal);

            // DateTime con Kind "Unspecified" + la zona horaria del trabajo -> se interpreta
            // explícitamente como hora local de esa zona antes de convertir a UTC, sin ambigüedad.
            DateTime nextLocalUnspecified = DateTime.SpecifyKind(nextLocal, DateTimeKind.Unspecified);
            DateTime nextUtc = TimeZoneInfo.ConvertTimeToUtc(nextLocalUnspecified, TimeZone);

            return new DateTimeOffset(nextUtc, TimeSpan.Zero);
        }
    }
}
