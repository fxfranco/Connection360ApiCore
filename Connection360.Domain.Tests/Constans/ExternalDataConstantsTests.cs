using Connection360.Domain.Constans;
using FluentAssertions;
using System.Reflection;
using Xunit;

namespace Connection360.Domain.Tests.Constans
{
    public class ExternalDataConstantsTests
    {
        [Fact]
        public void ExternalDataFields_TodasLasConstantesSonUnicasYNoVacias()
        {
            var values = typeof(ExternalDataFields)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral)
                .Select(f => (String)f.GetRawConstantValue()!)
                .ToList();

            values.Should().NotBeEmpty();
            values.Should().OnlyContain(v => !String.IsNullOrWhiteSpace(v));
            values.Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public void ExternalDataValues_TodasLasConstantesSonUnicasYNoVacias()
        {
            var values = typeof(ExternalDataValues)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral)
                .Select(f => (String)f.GetRawConstantValue()!)
                .ToList();

            values.Should().NotBeEmpty();
            values.Should().OnlyContain(v => !String.IsNullOrWhiteSpace(v));
            values.Should().OnlyHaveUniqueItems();
        }

        [Theory]
        [InlineData(ExternalDataValues.Import, "IMPO")]
        [InlineData(ExternalDataValues.Export, "EXPO")]
        [InlineData(ExternalDataValues.AirShipment, "AIR")]
        [InlineData(ExternalDataValues.OceanShipment, "SEA")]
        public void ExternalDataValues_ConservaLosValoresEsperadosPorContrato(String actual, String expected)
        {
            actual.Should().Be(expected);
        }
    }
}
