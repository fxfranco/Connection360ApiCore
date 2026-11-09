using Connection360.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Connection360.Domain.Tests.Entities
{
    public class DateParsingExtensionsTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ToDateTimeOrMin_CuandoCadenaEsNuloOVacia_RetornaMinValue(String? input)
        {
            input!.ToDateTimeOrMin().Should().Be(DateTime.MinValue);
        }

        [Theory]
        [InlineData("07/04/2024", 2024, 4, 7)]
        [InlineData("7/4/2024", 2024, 4, 7)]
        public void ToDateTimeOrMin_ConFormatoNumericoSimple_ParseaCorrectamente(String input, Int32 year, Int32 month, Int32 day)
        {
            DateTime result = input.ToDateTimeOrMin();

            result.Year.Should().Be(year);
            result.Month.Should().Be(month);
            result.Day.Should().Be(day);
        }

        [Fact]
        public void ToDateTimeOrMin_ConFormatoFechaYHora_ParseaCorrectamente()
        {
            DateTime result = "07/04/2024 00:00:00".ToDateTimeOrMin();

            result.Should().Be(new DateTime(2024, 4, 7, 0, 0, 0));
        }

        [Fact]
        public void ToDateTimeOrMin_ConMesEnTextoAbreviado_ParseaCorrectamente()
        {
            DateTime result = "2/oct/2025".ToDateTimeOrMin();

            result.Year.Should().Be(2025);
            result.Month.Should().Be(10);
            result.Day.Should().Be(2);
        }

        [Fact]
        public void ToDateTimeOrMin_ConEspaciosAlrededor_HaceTrimYParsea()
        {
            DateTime result = "  07/04/2024  ".ToDateTimeOrMin();

            result.Should().Be(new DateTime(2024, 4, 7));
        }

        [Fact]
        public void ToDateTimeOrMin_ConFormatoIso_UsaRespaldoDeCulturaInvariante()
        {
            DateTime result = "2024-04-07".ToDateTimeOrMin();

            result.Should().Be(new DateTime(2024, 4, 7));
        }

        [Fact]
        public void ToDateTimeOrMin_ConCadenaNoParseable_RetornaMinValue()
        {
            "esto-no-es-una-fecha".ToDateTimeOrMin().Should().Be(DateTime.MinValue);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("abc")]
        public void ToDouble_ConCadenaInvalida_RetornaCero(String? input)
        {
            input!.ToDouble().Should().Be(0d);
        }

        [Theory]
        [InlineData("123.45", 123.45)]
        [InlineData("0", 0d)]
        [InlineData("-10.5", -10.5)]
        public void ToDouble_ConCadenaValida_ParseaConCulturaInvariante(String input, Double expected)
        {
            input.ToDouble().Should().Be(expected);
        }
    }
}
