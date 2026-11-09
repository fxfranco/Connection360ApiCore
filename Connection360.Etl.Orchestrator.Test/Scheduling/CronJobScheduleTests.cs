using Connection360.Etl.Orchestrator.Scheduling;
using FluentAssertions;
using Xunit;

namespace Connection360.Etl.Orchestrator.Test.Scheduling
{
    public class CronJobScheduleTests
    {
        // Zona fija UTC-5 (sin horario de verano), equivalente a America/Bogota pero sin depender
        // de la base de datos de zonas horarias del sistema operativo.
        private static readonly TimeZoneInfo UtcMinus5 = TimeZoneInfo.CreateCustomTimeZone(
            "TEST_UTC_MINUS_5", TimeSpan.FromHours(-5), "UTC-5", "UTC-5");

        private static DateTimeOffset Utc(Int32 y, Int32 mo, Int32 d, Int32 h, Int32 mi, Int32 s = 0) =>
            new(y, mo, d, h, mi, s, TimeSpan.Zero);

        [Fact]
        public void Constructor_ExponeLaZonaHorariaRecibida()
        {
            var schedule = new CronJobSchedule(CronExpression.Parse("* * * * *"), UtcMinus5);

            schedule.TimeZone.Should().BeSameAs(UtcMinus5);
        }

        [Fact]
        public void GetNextOccurrenceUtc_ZonaUtc_CoincideConLaExpresion()
        {
            var schedule = new CronJobSchedule(CronExpression.Parse("0 8 * * *"), TimeZoneInfo.Utc);

            var next = schedule.GetNextOccurrenceUtc(Utc(2026, 10, 4, 7, 59, 30));

            next.Should().Be(Utc(2026, 10, 4, 8, 0));
            next.Offset.Should().Be(TimeSpan.Zero);
        }

        [Fact]
        public void GetNextOccurrenceUtc_ZonaConOffset_InterpretaLaExpresionEnHoraLocalYDevuelveUtc()
        {
            var schedule = new CronJobSchedule(CronExpression.Parse("0 8 * * *"), UtcMinus5);

            // 12:00Z = 07:00 local -> las 08:00 locales son las 13:00Z del mismo día.
            var next = schedule.GetNextOccurrenceUtc(Utc(2026, 10, 4, 12, 0));

            next.Should().Be(Utc(2026, 10, 4, 13, 0));
            next.Offset.Should().Be(TimeSpan.Zero);
        }

        [Fact]
        public void GetNextOccurrenceUtc_InstanteQueYaCoincide_AvanzaAlDiaSiguiente()
        {
            var schedule = new CronJobSchedule(CronExpression.Parse("0 8 * * *"), UtcMinus5);

            var next = schedule.GetNextOccurrenceUtc(Utc(2026, 10, 4, 13, 0));

            next.Should().Be(Utc(2026, 10, 5, 13, 0));
        }

        [Fact]
        public void GetNextOccurrenceUtc_LaFechaLocalCambiaDeDiaRespectoAUtc_SeEvaluaEnLaFechaLocal()
        {
            // 02:00Z del 5 de octubre = 21:00 local del 4 de octubre. Siguiente "0 22 * * *" local = 22:00 local
            // del 4 de octubre = 03:00Z del 5.
            var schedule = new CronJobSchedule(CronExpression.Parse("0 22 * * *"), UtcMinus5);

            var next = schedule.GetNextOccurrenceUtc(Utc(2026, 10, 5, 2, 0));

            next.Should().Be(Utc(2026, 10, 5, 3, 0));
        }

        [Fact]
        public void GetNextOccurrenceUtc_ZonaConOffsetPositivo_ConviertePorRestaDeOffset()
        {
            var utcPlus2 = TimeZoneInfo.CreateCustomTimeZone("TEST_UTC_PLUS_2", TimeSpan.FromHours(2), "UTC+2", "UTC+2");
            var schedule = new CronJobSchedule(CronExpression.Parse("30 9 * * *"), utcPlus2);

            // 06:00Z = 08:00 local -> 09:30 local = 07:30Z.
            var next = schedule.GetNextOccurrenceUtc(Utc(2026, 10, 4, 6, 0));

            next.Should().Be(Utc(2026, 10, 4, 7, 30));
        }

        [Fact]
        public void GetNextOccurrenceUtc_ExpresionImposible_PropagaInvalidOperationException()
        {
            var schedule = new CronJobSchedule(CronExpression.Parse("0 0 31 2 *"), TimeZoneInfo.Utc);

            Action act = () => schedule.GetNextOccurrenceUtc(Utc(2026, 1, 1, 0, 0));

            act.Should().Throw<InvalidOperationException>();
        }
    }
}
