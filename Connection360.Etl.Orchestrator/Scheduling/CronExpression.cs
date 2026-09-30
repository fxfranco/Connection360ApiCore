namespace Connection360.Etl.Orchestrator.Scheduling
{
    /// <summary>
    /// Parser y evaluador de expresiones cron estándar de 5 campos (minuto hora día-mes mes
    /// día-semana). Implementación propia, deliberadamente simple, sin dependencias externas de
    /// NuGet (ver el comentario en Connection360.Etl.Orchestrator.csproj): recorre minuto a minuto
    /// desde el punto de partida hasta encontrar el primero que satisface los 5 campos, en vez de
    /// "saltar" directamente al próximo valor válido de cada campo como hacen los motores más
    /// sofisticados (Cronos, Quartz). Para el caso de uso de este orquestador -unos pocos trabajos,
    /// calculando su próxima ejecución cada vez que termina la anterior- el costo de este recorrido
    /// es insignificante.
    /// <para>
    /// Soporta: <c>*</c> (cualquier valor), listas separadas por comas (<c>1,15,30</c>), rangos
    /// (<c>1-5</c>) y pasos (<c>*/15</c>, <c>1-30/5</c>). NO soporta nombres de mes/día de semana
    /// (solo numéricos), ni rangos que "envuelven" (ej. <c>22-2</c> para horas) ni campos
    /// especiales tipo <c>L</c>/<c>W</c>/<c>#</c> de otros motores.
    /// </para>
    /// </summary>
    public sealed class CronExpression
    {
        private const Int32 MaxIterations = 4 * 366 * 24 * 60; // ~4 años recorridos minuto a minuto: cota de seguridad ante expresiones imposibles.

        private readonly Boolean[] _minutes;
        private readonly Boolean[] _hours;
        private readonly Boolean[] _daysOfMonth;
        private readonly Boolean[] _months;
        private readonly Boolean[] _daysOfWeek;
        private readonly Boolean _restrictedDayOfMonth;
        private readonly Boolean _restrictedDayOfWeek;
        private readonly String _raw;

        private CronExpression(
            String raw,
            Boolean[] minutes,
            Boolean[] hours,
            Boolean[] daysOfMonth,
            Boolean[] months,
            Boolean[] daysOfWeek,
            Boolean restrictedDayOfMonth,
            Boolean restrictedDayOfWeek)
        {
            _raw = raw;
            _minutes = minutes;
            _hours = hours;
            _daysOfMonth = daysOfMonth;
            _months = months;
            _daysOfWeek = daysOfWeek;
            _restrictedDayOfMonth = restrictedDayOfMonth;
            _restrictedDayOfWeek = restrictedDayOfWeek;
        }

        /// <exception cref="FormatException">La expresión no tiene 5 campos, o alguno es inválido/está fuera de rango.</exception>
        public static CronExpression Parse(String expression)
        {
            if (String.IsNullOrWhiteSpace(expression))
                throw new FormatException("La expresión cron no puede estar vacía.");

            var fields = expression.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length != 5)
                throw new FormatException($"La expresión cron '{expression}' debe tener 5 campos (minuto hora día-mes mes día-semana); tiene {fields.Length}.");

            var minutes = ParseField(fields[0], 0, 59, expression);
            var hours = ParseField(fields[1], 0, 23, expression);
            var daysOfMonth = ParseField(fields[2], 1, 31, expression);
            var months = ParseField(fields[3], 1, 12, expression);
            var daysOfWeekRaw = ParseField(fields[4], 0, 7, expression);

            // Día de semana: tanto 0 como 7 representan domingo (convención cron estándar); ambos
            // se normalizan al índice 0 de un arreglo de 7 posiciones (0=domingo ... 6=sábado, igual
            // que System.DayOfWeek).
            var daysOfWeek = new Boolean[7];
            for (Int32 i = 0; i <= 7; i++)
            {
                if (daysOfWeekRaw[i])
                    daysOfWeek[i % 7] = true;
            }

            return new CronExpression(
                expression,
                minutes,
                hours,
                daysOfMonth,
                months,
                daysOfWeek,
                restrictedDayOfMonth: fields[2] != "*",
                restrictedDayOfWeek: fields[4] != "*");
        }

        private static Boolean[] ParseField(String field, Int32 min, Int32 max, String fullExpression)
        {
            var values = new Boolean[max + 1];

            foreach (var part in field.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                Int32 step = 1;
                String rangePart = part;

                Int32 slashIndex = part.IndexOf('/');
                if (slashIndex >= 0)
                {
                    rangePart = part[..slashIndex];
                    if (!Int32.TryParse(part[(slashIndex + 1)..], out step) || step <= 0)
                        throw new FormatException($"Paso inválido en '{part}' dentro de la expresión cron '{fullExpression}'.");
                }

                Int32 rangeStart;
                Int32 rangeEnd;

                if (rangePart == "*")
                {
                    rangeStart = min;
                    rangeEnd = max;
                }
                else
                {
                    Int32 dashIndex = rangePart.IndexOf('-');
                    if (dashIndex >= 0)
                    {
                        if (!Int32.TryParse(rangePart[..dashIndex], out rangeStart) || !Int32.TryParse(rangePart[(dashIndex + 1)..], out rangeEnd))
                            throw new FormatException($"Rango inválido en '{part}' dentro de la expresión cron '{fullExpression}'.");
                    }
                    else
                    {
                        if (!Int32.TryParse(rangePart, out rangeStart))
                            throw new FormatException($"Valor inválido en '{part}' dentro de la expresión cron '{fullExpression}'.");
                        rangeEnd = rangeStart;
                    }
                }

                if (rangeStart < min || rangeEnd > max || rangeStart > rangeEnd)
                    throw new FormatException($"El campo '{part}' está fuera de rango ({min}-{max}) en la expresión cron '{fullExpression}'.");

                for (Int32 v = rangeStart; v <= rangeEnd; v += step)
                {
                    values[v] = true;
                }
            }

            return values;
        }

        /// <summary>
        /// Calcula la próxima fecha/hora (sin componente de segundos) que satisface la expresión,
        /// estrictamente posterior a <paramref name="after"/>. La fecha devuelta tiene el mismo
        /// <see cref="DateTimeKind"/> que <paramref name="after"/>: esta clase no conoce zonas
        /// horarias, esa responsabilidad es de <see cref="CronJobSchedule"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// No se encontró ninguna ocurrencia dentro de los próximos ~4 años (expresión imposible,
        /// por ejemplo el día 31 de un mes que nunca lo tiene combinado con un día de semana que
        /// nunca cae ahí).
        /// </exception>
        public DateTime GetNextOccurrence(DateTime after)
        {
            DateTime candidate = new DateTime(after.Year, after.Month, after.Day, after.Hour, after.Minute, 0, after.Kind).AddMinutes(1);

            for (Int32 i = 0; i < MaxIterations; i++)
            {
                if (Matches(candidate))
                    return candidate;

                candidate = candidate.AddMinutes(1);
            }

            throw new InvalidOperationException($"No se encontró ninguna ocurrencia futura para la expresión cron '{_raw}' (¿es una combinación de campos imposible, como el día 31 de un mes que nunca lo tiene?).");
        }

        private Boolean Matches(DateTime candidate)
        {
            if (!_minutes[candidate.Minute]) return false;
            if (!_hours[candidate.Hour]) return false;
            if (!_months[candidate.Month]) return false;

            Boolean domMatches = _daysOfMonth[candidate.Day];
            Boolean dowMatches = _daysOfWeek[(Int32)candidate.DayOfWeek];

            // Regla estándar de cron: si AMBOS campos de día están restringidos (no "*"), la fecha
            // es válida si cumple CUALQUIERA de los dos (OR). Si solo uno está restringido (o
            // ninguno), manda ese (el otro, al ser "*", siempre es verdadero y no afecta el AND).
            if (_restrictedDayOfMonth && _restrictedDayOfWeek)
                return domMatches || dowMatches;

            return domMatches && dowMatches;
        }
    }
}
