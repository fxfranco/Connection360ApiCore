using Connection360.Etl.Orchestrator.Scheduling;
using FluentAssertions;
using Xunit;

namespace Connection360.Etl.Orchestrator.Test.Scheduling
{
    public class CronExpressionTests
    {
        private static DateTime Utc(Int32 y, Int32 mo, Int32 d, Int32 h, Int32 mi, Int32 s = 0) =>
            new(y, mo, d, h, mi, s, DateTimeKind.Utc);

        // ---------- Parse: expresiones inválidas ----------

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Parse_ExpresionNulaVaciaOEspacios_LanzaFormatException(String? expression)
        {
            Action act = () => CronExpression.Parse(expression!);

            act.Should().Throw<FormatException>().WithMessage("*vacía*");
        }

        [Theory]
        [InlineData("* * * *", 4)]
        [InlineData("* * * * * *", 6)]
        [InlineData("*", 1)]
        public void Parse_CantidadDeCamposDistintaDeCinco_LanzaFormatException(String expression, Int32 fields)
        {
            Action act = () => CronExpression.Parse(expression);

            act.Should().Throw<FormatException>().WithMessage($"*5 campos*tiene {fields}*");
        }

        [Theory]
        [InlineData("*/0 * * * *")]
        [InlineData("*/-1 * * * *")]
        [InlineData("*/x * * * *")]
        [InlineData("*/ * * * *")]
        public void Parse_PasoInvalido_LanzaFormatException(String expression)
        {
            Action act = () => CronExpression.Parse(expression);

            act.Should().Throw<FormatException>().WithMessage("*Paso inválido*");
        }

        [Theory]
        [InlineData("a-b * * * *")]
        [InlineData("1- * * * *")]
        [InlineData("-5 * * * *")]
        public void Parse_RangoNoNumerico_LanzaFormatException(String expression)
        {
            Action act = () => CronExpression.Parse(expression);

            act.Should().Throw<FormatException>().WithMessage("*Rango inválido*");
        }

        [Theory]
        [InlineData("x * * * *")]
        [InlineData("1.5 * * * *")]
        public void Parse_ValorNoNumerico_LanzaFormatException(String expression)
        {
            Action act = () => CronExpression.Parse(expression);

            act.Should().Throw<FormatException>().WithMessage("*Valor inválido*");
        }

        [Theory]
        [InlineData("60 * * * *")]   // minuto > 59
        [InlineData("* 24 * * *")]   // hora > 23
        [InlineData("* * 0 * *")]    // día del mes < 1
        [InlineData("* * 32 * *")]   // día del mes > 31
        [InlineData("* * * 0 *")]    // mes < 1
        [InlineData("* * * 13 *")]   // mes > 12
        [InlineData("* * * * 8")]    // día de semana > 7
        [InlineData("5-3 * * * *")]  // rango invertido
        [InlineData("* 22-2 * * *")] // rango que "envuelve": no soportado
        [InlineData("0-60 * * * *")] // fin fuera de rango
        public void Parse_ValorFueraDeRango_LanzaFormatException(String expression)
        {
            Action act = () => CronExpression.Parse(expression);

            act.Should().Throw<FormatException>().WithMessage("*fuera de rango*");
        }

        [Fact]
        public void Parse_MensajeDeErrorIncluyeLaExpresionOriginal()
        {
            Action act = () => CronExpression.Parse("99 * * * *");

            act.Should().Throw<FormatException>().WithMessage("*'99 * * * *'*");
        }

        // ---------- Parse: expresiones válidas ----------

        [Theory]
        [InlineData("* * * * *")]
        [InlineData("*/15 * * * *")]
        [InlineData("0 8 * * *")]
        [InlineData("0 8-18 * * 1-5")]
        [InlineData("1,15,30 * * * *")]
        [InlineData("0-30/10 * * * *")]
        [InlineData("0 0 1 1 *")]
        [InlineData("0 0 * * 7")]
        [InlineData("  */5    *  * *   *  ")]
        public void Parse_ExpresionValida_NoLanza(String expression)
        {
            Action act = () => CronExpression.Parse(expression);

            act.Should().NotThrow();
        }

        // ---------- GetNextOccurrence ----------

        [Fact]
        public void GetNextOccurrence_CadaMinuto_DevuelveElMinutoSiguienteSinSegundos()
        {
            var cron = CronExpression.Parse("* * * * *");

            var next = cron.GetNextOccurrence(Utc(2026, 10, 4, 12, 7, 45));

            next.Should().Be(Utc(2026, 10, 4, 12, 8, 0));
        }

        [Fact]
        public void GetNextOccurrence_EsEstrictamentePosterior_SiElInstanteYaCoincideAvanzaAlSiguiente()
        {
            var cron = CronExpression.Parse("*/15 * * * *");

            cron.GetNextOccurrence(Utc(2026, 10, 4, 12, 15, 0)).Should().Be(Utc(2026, 10, 4, 12, 30, 0));
            cron.GetNextOccurrence(Utc(2026, 10, 4, 12, 7, 45)).Should().Be(Utc(2026, 10, 4, 12, 15, 0));
        }

        [Fact]
        public void GetNextOccurrence_PasoCruzandoLaHora_RodaALaSiguienteHora()
        {
            var cron = CronExpression.Parse("*/15 * * * *");

            cron.GetNextOccurrence(Utc(2026, 10, 4, 12, 45, 0)).Should().Be(Utc(2026, 10, 4, 13, 0, 0));
        }

        [Fact]
        public void GetNextOccurrence_HoraFija_PasaAlDiaSiguienteSiYaPaso()
        {
            var cron = CronExpression.Parse("0 8 * * *");

            cron.GetNextOccurrence(Utc(2026, 1, 1, 8, 0, 0)).Should().Be(Utc(2026, 1, 2, 8, 0, 0));
            cron.GetNextOccurrence(Utc(2026, 1, 1, 7, 59, 59)).Should().Be(Utc(2026, 1, 1, 8, 0, 0));
        }

        [Fact]
        public void GetNextOccurrence_FinDeAnio_RodaAlAnioSiguiente()
        {
            var cron = CronExpression.Parse("0 0 1 1 *");

            cron.GetNextOccurrence(Utc(2026, 6, 15, 10, 0, 0)).Should().Be(Utc(2027, 1, 1, 0, 0, 0));
        }

        [Fact]
        public void GetNextOccurrence_Lista_RecorreCadaValorDeLaLista()
        {
            var cron = CronExpression.Parse("1,15,30 * * * *");

            cron.GetNextOccurrence(Utc(2026, 10, 4, 12, 0, 0)).Should().Be(Utc(2026, 10, 4, 12, 1, 0));
            cron.GetNextOccurrence(Utc(2026, 10, 4, 12, 1, 0)).Should().Be(Utc(2026, 10, 4, 12, 15, 0));
            cron.GetNextOccurrence(Utc(2026, 10, 4, 12, 15, 0)).Should().Be(Utc(2026, 10, 4, 12, 30, 0));
            cron.GetNextOccurrence(Utc(2026, 10, 4, 12, 30, 0)).Should().Be(Utc(2026, 10, 4, 13, 1, 0));
        }

        [Fact]
        public void GetNextOccurrence_Rango_SoloCoincideDentroDelRango()
        {
            var cron = CronExpression.Parse("10-12 * * * *");

            cron.GetNextOccurrence(Utc(2026, 10, 4, 12, 0, 0)).Should().Be(Utc(2026, 10, 4, 12, 10, 0));
            cron.GetNextOccurrence(Utc(2026, 10, 4, 12, 12, 0)).Should().Be(Utc(2026, 10, 4, 13, 10, 0));
        }

        [Fact]
        public void GetNextOccurrence_RangoConPaso_UsaSoloLosValoresDelPaso()
        {
            var cron = CronExpression.Parse("0-30/10 * * * *");

            var first = cron.GetNextOccurrence(Utc(2026, 10, 4, 12, 0, 0));
            var second = cron.GetNextOccurrence(first);
            var third = cron.GetNextOccurrence(second);
            var fourth = cron.GetNextOccurrence(third);
            var fifth = cron.GetNextOccurrence(fourth);

            first.Minute.Should().Be(10);
            second.Minute.Should().Be(20);
            third.Minute.Should().Be(30);
            fourth.Should().Be(Utc(2026, 10, 4, 13, 0, 0));
            fifth.Minute.Should().Be(10);
        }

        [Fact]
        public void GetNextOccurrence_ValorUnicoConPaso_SoloCoincideEnEseValor()
        {
            var cron = CronExpression.Parse("5/10 * * * *");

            cron.GetNextOccurrence(Utc(2026, 10, 4, 12, 0, 0)).Should().Be(Utc(2026, 10, 4, 12, 5, 0));
            cron.GetNextOccurrence(Utc(2026, 10, 4, 12, 5, 0)).Should().Be(Utc(2026, 10, 4, 13, 5, 0));
        }

        [Fact]
        public void GetNextOccurrence_DiaDeSemana_SoloCoincideEnEseDia()
        {
            var cron = CronExpression.Parse("0 9 * * 1"); // lunes

            // 2026-10-04 es domingo.
            var next = cron.GetNextOccurrence(Utc(2026, 10, 4, 12, 0, 0));

            next.Should().Be(Utc(2026, 10, 5, 9, 0, 0));
            next.DayOfWeek.Should().Be(DayOfWeek.Monday);
        }

        [Theory]
        [InlineData("0 0 * * 0")]
        [InlineData("0 0 * * 7")]
        public void GetNextOccurrence_CeroYSieteRepresentanDomingo(String expression)
        {
            var cron = CronExpression.Parse(expression);

            var next = cron.GetNextOccurrence(Utc(2026, 10, 5, 0, 0, 0)); // lunes

            next.Should().Be(Utc(2026, 10, 11, 0, 0, 0));
            next.DayOfWeek.Should().Be(DayOfWeek.Sunday);
        }

        [Fact]
        public void GetNextOccurrence_RangoDeDiasDeSemanaLunesAViernes_SaltaElFinDeSemana()
        {
            var cron = CronExpression.Parse("0 8 * * 1-5");

            // Viernes 2026-10-09 a las 9:00 -> el siguiente es lunes 2026-10-12 a las 8:00.
            var next = cron.GetNextOccurrence(Utc(2026, 10, 9, 9, 0, 0));

            next.Should().Be(Utc(2026, 10, 12, 8, 0, 0));
        }

        [Fact]
        public void GetNextOccurrence_DiaDelMesRestringido_SoloCoincideEnEseDia()
        {
            var cron = CronExpression.Parse("0 0 15 * *");

            cron.GetNextOccurrence(Utc(2026, 10, 16, 0, 0, 0)).Should().Be(Utc(2026, 11, 15, 0, 0, 0));
        }

        [Fact]
        public void GetNextOccurrence_DiaDelMesYDiaDeSemanaRestringidos_AplicaOrEstandarDeCron()
        {
            // "viernes 13" en cron estándar significa: el día 13 O cualquier viernes.
            var cron = CronExpression.Parse("0 0 13 * 5");

            // Desde el lunes 2026-10-05: el primer viernes es el 9 (antes del 13, que cae martes).
            var first = cron.GetNextOccurrence(Utc(2026, 10, 5, 0, 0, 0));
            first.Should().Be(Utc(2026, 10, 9, 0, 0, 0));

            // Desde el sábado 10: el día 13 (martes) coincide aunque no sea viernes.
            var second = cron.GetNextOccurrence(Utc(2026, 10, 10, 0, 0, 0));
            second.Should().Be(Utc(2026, 10, 13, 0, 0, 0));
            second.DayOfWeek.Should().NotBe(DayOfWeek.Friday);
        }

        [Fact]
        public void GetNextOccurrence_MesRestringido_SaltaAlMesIndicado()
        {
            var cron = CronExpression.Parse("30 6 1 6 *");

            cron.GetNextOccurrence(Utc(2026, 10, 4, 0, 0, 0)).Should().Be(Utc(2027, 6, 1, 6, 30, 0));
        }

        [Fact]
        public void GetNextOccurrence_29DeFebrero_EncuentraElProximoAnioBisiesto()
        {
            var cron = CronExpression.Parse("30 2 29 2 *");

            cron.GetNextOccurrence(Utc(2026, 1, 1, 0, 0, 0)).Should().Be(Utc(2028, 2, 29, 2, 30, 0));
        }

        [Theory]
        [InlineData(DateTimeKind.Utc)]
        [InlineData(DateTimeKind.Local)]
        [InlineData(DateTimeKind.Unspecified)]
        public void GetNextOccurrence_ConservaElKindDeLaFechaRecibida(DateTimeKind kind)
        {
            var cron = CronExpression.Parse("*/5 * * * *");
            var after = new DateTime(2026, 10, 4, 12, 0, 0, kind);

            var next = cron.GetNextOccurrence(after);

            next.Kind.Should().Be(kind);
            next.Minute.Should().Be(5);
        }

        [Fact]
        public void GetNextOccurrence_ExpresionImposible_LanzaInvalidOperationException()
        {
            // El 31 de febrero nunca existe.
            var cron = CronExpression.Parse("0 0 31 2 *");

            Action act = () => cron.GetNextOccurrence(Utc(2026, 1, 1, 0, 0, 0));

            act.Should().Throw<InvalidOperationException>().WithMessage("*0 0 31 2 **");
        }
    }
}
