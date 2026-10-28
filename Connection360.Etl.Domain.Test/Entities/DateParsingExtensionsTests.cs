using System.Globalization;
using Connection360.Etl.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Connection360.Etl.Domain.Test.Entities
{
    public class DateParsingExtensionsTests
    {
        private static readonly CultureInfo Es = new("es-CO");

        // ---------- ToDateTimeOrMin ----------

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\n")]
        public void ToDateTimeOrMin_VacioONulo_RetornaMinValue(String? input)
        {
            input!.ToDateTimeOrMin().Should().Be(DateTime.MinValue);
        }

        [Theory]
        [InlineData("texto no fecha")]
        [InlineData("32/13/2024")]
        [InlineData("abc/def/2024")]
        [InlineData("--")]
        public void ToDateTimeOrMin_TextoInvalido_RetornaMinValue(String input)
        {
            input.ToDateTimeOrMin().Should().Be(DateTime.MinValue);
        }

        [Theory]
        [InlineData("07/04/2024", 2024, 4, 7)]
        [InlineData("7/4/2024", 2024, 4, 7)]
        [InlineData("31/12/2025", 2025, 12, 31)]
        [InlineData("01/01/2000", 2000, 1, 1)]
        public void ToDateTimeOrMin_SoloFechaDiaMes_InterpretaDiaPrimero(String input, Int32 y, Int32 m, Int32 d)
        {
            input.ToDateTimeOrMin().Should().Be(new DateTime(y, m, d));
        }

        [Fact]
        public void ToDateTimeOrMin_FechaConEspaciosAlrededor_LosIgnora()
        {
            "  07/04/2024  ".ToDateTimeOrMin().Should().Be(new DateTime(2024, 4, 7));
        }

        [Theory]
        [InlineData("07/04/2024 00:00:00", 2024, 4, 7, 0, 0, 0)]
        [InlineData("7/4/2024 13:45:10", 2024, 4, 7, 13, 45, 10)]
        [InlineData("25/12/2024 23:59:59", 2024, 12, 25, 23, 59, 59)]
        public void ToDateTimeOrMin_ConHora24h_ParseaFechaYHora(String input, Int32 y, Int32 mo, Int32 d, Int32 h, Int32 mi, Int32 s)
        {
            input.ToDateTimeOrMin().Should().Be(new DateTime(y, mo, d, h, mi, s));
        }

        [Fact]
        public void ToDateTimeOrMin_ConHoraPm_UsaDesignadorDeLaCulturaEsCo()
        {
            var pm = Es.DateTimeFormat.PMDesignator;
            if (String.IsNullOrEmpty(pm))
                return; // Cultura sin designador (p. ej. globalización invariante): no aplica.

            var result = $"7/4/2024 12:30:00 {pm}".ToDateTimeOrMin();

            result.Should().Be(new DateTime(2024, 4, 7, 12, 30, 0));
        }

        [Fact]
        public void ToDateTimeOrMin_ConHoraAm_UsaDesignadorDeLaCulturaEsCo()
        {
            var am = Es.DateTimeFormat.AMDesignator;
            if (String.IsNullOrEmpty(am))
                return;

            var result = $"07/04/2024 09:15:20 {am}".ToDateTimeOrMin();

            result.Should().Be(new DateTime(2024, 4, 7, 9, 15, 20));
        }

        [Fact]
        public void ToDateTimeOrMin_MesAbreviadoEnTexto_ParseaConNombreDeMesDeLaCultura()
        {
            var oct = Es.DateTimeFormat.AbbreviatedMonthNames[9];

            $"2/{oct}/2025".ToDateTimeOrMin().Should().Be(new DateTime(2025, 10, 2));
            $"02/{oct}/2025".ToDateTimeOrMin().Should().Be(new DateTime(2025, 10, 2));
        }

        [Fact]
        public void ToDateTimeOrMin_MesAbreviadoConHora_ParseaFechaYHora()
        {
            var oct = Es.DateTimeFormat.AbbreviatedMonthNames[9];

            $"2/{oct}/2025 14:05:06".ToDateTimeOrMin().Should().Be(new DateTime(2025, 10, 2, 14, 5, 6));
            $"02/{oct}/2025 14:05:06".ToDateTimeOrMin().Should().Be(new DateTime(2025, 10, 2, 14, 5, 6));
        }

        [Theory]
        [InlineData("2024-04-07", 2024, 4, 7)]
        [InlineData("2024-04-07T10:20:30", 2024, 4, 7)]
        public void ToDateTimeOrMin_FormatoIso_UsaRespaldoInvariante(String input, Int32 y, Int32 m, Int32 d)
        {
            var result = input.ToDateTimeOrMin();

            result.Year.Should().Be(y);
            result.Month.Should().Be(m);
            result.Day.Should().Be(d);
        }

        [Fact]
        public void ToDateTimeOrMin_MesMayorADoce_UsaRespaldoInvarianteMesDiaAnio()
        {
            // 12/25/2024 no cumple dd/MM/yyyy (mes 25) -> cae al parseo invariante (MM/dd/yyyy).
            "12/25/2024".ToDateTimeOrMin().Should().Be(new DateTime(2024, 12, 25));
        }

        // ---------- ToNullableDate ----------

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData("no es fecha")]
        public void ToNullableDate_VacioOInvalido_RetornaNull(String? input)
        {
            input!.ToNullableDate().Should().BeNull();
        }

        [Fact]
        public void ToNullableDate_FechaValida_RetornaFecha()
        {
            "07/04/2024".ToNullableDate().Should().Be(new DateTime(2024, 4, 7));
        }

        [Fact]
        public void ToNullableDate_FechaIgualAMinValueTexto_RetornaNull()
        {
            "01/01/0001".ToNullableDate().Should().BeNull();
        }

        // ---------- ToDouble ----------

        [Theory]
        [InlineData("1.5", 1.5)]
        [InlineData("-3.25", -3.25)]
        [InlineData("1,234.5", 1234.5)]
        [InlineData("0", 0.0)]
        [InlineData("  42  ", 42.0)]
        public void ToDouble_NumeroValido_ParseaConCulturaInvariante(String input, Double expected)
        {
            input.ToDouble().Should().Be(expected);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("abc")]
        [InlineData("1.2.3")]
        public void ToDouble_Invalido_RetornaCero(String? input)
        {
            input!.ToDouble().Should().Be(0d);
        }

        // ---------- ToDecimalOrDefault ----------

        [Theory]
        [InlineData("1.5", "1.5")]
        [InlineData("1234.56", "1234.56")]
        [InlineData("1,234.56", "1234.56")]
        [InlineData("-10", "-10")]
        [InlineData("0.001", "0.001")]
        public void ToDecimalOrDefault_NumeroValido_Parsea(String input, String expected)
        {
            input.ToDecimalOrDefault().Should().Be(Decimal.Parse(expected, CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("n/a")]
        public void ToDecimalOrDefault_Invalido_RetornaCero(String? input)
        {
            input!.ToDecimalOrDefault().Should().Be(0m);
        }

        // ---------- ToInt32OrDefault ----------

        [Theory]
        [InlineData("5", 5)]
        [InlineData("-7", -7)]
        [InlineData("1,000", 1000)]
        [InlineData(" 12 ", 12)]
        [InlineData("2147483647", Int32.MaxValue)]
        public void ToInt32OrDefault_EnteroValido_Parsea(String input, Int32 expected)
        {
            input.ToInt32OrDefault().Should().Be(expected);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("abc")]
        [InlineData("1.5")]
        [InlineData("2147483648")]
        public void ToInt32OrDefault_InvalidoOFueraDeRango_RetornaCero(String? input)
        {
            input!.ToInt32OrDefault().Should().Be(0);
        }

        // ---------- ToInt64OrDefault ----------

        [Theory]
        [InlineData("5", 5L)]
        [InlineData("-7", -7L)]
        [InlineData("9223372036854775807", Int64.MaxValue)]
        [InlineData("2147483648", 2147483648L)]
        public void ToInt64OrDefault_EnteroValido_Parsea(String input, Int64 expected)
        {
            input.ToInt64OrDefault().Should().Be(expected);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("x")]
        [InlineData("1.5")]
        [InlineData("9223372036854775808")]
        public void ToInt64OrDefault_InvalidoOFueraDeRango_RetornaCero(String? input)
        {
            input!.ToInt64OrDefault().Should().Be(0L);
        }
    }
}
