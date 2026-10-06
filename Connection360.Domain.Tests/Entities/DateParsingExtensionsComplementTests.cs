using Connection360.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Connection360.Domain.Tests.Entities
{
    public class DateParsingExtensionsComplementTests
    {
        [Theory]
        [InlineData("31/12/2024 23:59:58", 2024, 12, 31, 23, 59, 58)]
        [InlineData("7/4/2024 08:05:01", 2024, 4, 7, 8, 5, 1)]
        [InlineData("2/oct/2025", 2025, 10, 2, 0, 0, 0)]
        [InlineData("02/oct/2025", 2025, 10, 2, 0, 0, 0)]
        [InlineData("2/oct/2025 10:20:30", 2025, 10, 2, 10, 20, 30)]
        [InlineData("02/oct/2025 10:20:30", 2025, 10, 2, 10, 20, 30)]
        [InlineData("2025-04-07T10:20:30", 2025, 4, 7, 10, 20, 30)]
        [InlineData("2025-04-07 10:20:30", 2025, 4, 7, 10, 20, 30)]
        public void ToDateTimeOrMin_ConFormatosSoportados_ParseaFechaYHora(String input, Int32 y, Int32 mo, Int32 d, Int32 h, Int32 mi, Int32 s)
        {
            input.ToDateTimeOrMin().Should().Be(new DateTime(y, mo, d, h, mi, s));
        }

        [Theory]
        [InlineData("32/13/2024")]
        [InlineData("00/00/0000")]
        [InlineData("texto cualquiera")]
        [InlineData("99/99/9999 99:99:99")]
        public void ToDateTimeOrMin_ConFechasImposibles_RetornaMinValue(String input)
        {
            input.ToDateTimeOrMin().Should().Be(DateTime.MinValue);
        }

        [Fact]
        public void ToDateTimeOrMin_ConDiaMesAmbiguo_InterpretaDiaPrimero()
        {
            "03/04/2024".ToDateTimeOrMin().Should().Be(new DateTime(2024, 4, 3));
        }

        [Theory]
        [InlineData("1,234.5", 1234.5)]
        [InlineData("  12.5  ", 12.5)]
        [InlineData("1e3", 1000d)]
        [InlineData("+7", 7d)]
        public void ToDouble_ConFormatosNumericosPermitidos_ParseaConCulturaInvariante(String input, Double expected)
        {
            input.ToDouble().Should().Be(expected);
        }

        [Theory]
        [InlineData("1,5x")]
        [InlineData("   ")]
        [InlineData("NaN-text")]
        public void ToDouble_ConTextoNoNumerico_RetornaCero(String input)
        {
            input.ToDouble().Should().Be(0d);
        }

        [Fact]
        public void ToDouble_ConSeparadorDecimalDeCulturaEspanola_NoLoInterpretaComoDecimal()
        {
            // Con cultura invariante la coma es separador de miles: "1,5" => 15.
            "1,5".ToDouble().Should().Be(15d);
        }
    }
}
