using System;
using Xunit;
using Connection360.Domain.Time;

namespace Connection360.Domain.Tests.Time
{
    public class ColombiaClockTests
    {
        [Fact]
        public void Now_EsLaHoraDeColombia_CincoHorasAntesDeUtc()
        {
            DateTime before = DateTime.UtcNow;
            DateTime now = ColombiaClock.Now;
            DateTime after = DateTime.UtcNow;

            Assert.InRange(now, before.AddHours(-5).AddSeconds(-1), after.AddHours(-5).AddSeconds(1));
        }

        [Fact]
        public void Now_TieneKindUnspecified()
        {
            Assert.Equal(DateTimeKind.Unspecified, ColombiaClock.Now.Kind);
        }

        [Fact]
        public void NowAsUtcKind_TieneKindUtc_YLaMismaHoraDeColombia()
        {
            DateTime before = ColombiaClock.Now;
            DateTime value = ColombiaClock.NowAsUtcKind;
            DateTime after = ColombiaClock.Now;

            Assert.Equal(DateTimeKind.Utc, value.Kind);
            Assert.InRange(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), before.AddSeconds(-1), after.AddSeconds(1));
        }

        [Theory]
        [InlineData(2026, 1, 1, 3, 0, 0, 2025, 12, 31, 22, 0, 0)]
        [InlineData(2026, 7, 15, 17, 30, 0, 2026, 7, 15, 12, 30, 0)]
        [InlineData(2026, 3, 1, 5, 0, 0, 2026, 3, 1, 0, 0, 0)]
        public void FromUtc_RestaCincoHoras_SinHorarioDeVerano(
            Int32 y, Int32 mo, Int32 d, Int32 h, Int32 mi, Int32 s,
            Int32 ey, Int32 emo, Int32 ed, Int32 eh, Int32 emi, Int32 es)
        {
            DateTime result = ColombiaClock.FromUtc(new DateTime(y, mo, d, h, mi, s, DateTimeKind.Utc));

            Assert.Equal(new DateTime(ey, emo, ed, eh, emi, es), result);
            Assert.Equal(DateTimeKind.Unspecified, result.Kind);
        }

        [Fact]
        public void FromUtc_ConKindUnspecified_LoTrataComoUtc()
        {
            DateTime result = ColombiaClock.FromUtc(new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Unspecified));

            Assert.Equal(new DateTime(2026, 6, 1, 5, 0, 0), result);
        }
    }
}
