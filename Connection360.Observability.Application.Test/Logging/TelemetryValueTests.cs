using Connection360.Observability.Application;
using FluentAssertions;
using Xunit;

namespace Connection360.Observability.Application.Test.Logging
{
    public class TelemetryValueTests
    {
        private enum Color { Rojo }

        [Fact]
        public void Normalize_Nulo_DevuelveNulo() => TelemetryValue.Normalize(null, 100).Should().BeNull();

        [Fact]
        public void Normalize_TextoLargo_LoTrunca() => TelemetryValue.Normalize(new String('a', 50), 10).Should().Be(new String('a', 10));

        [Fact]
        public void Normalize_TextoCorto_NoLoModifica() => TelemetryValue.Normalize("hola", 10).Should().Be("hola");

        [Theory]
        [InlineData(true)]
        [InlineData(5)]
        [InlineData(5L)]
        [InlineData(1.5)]
        public void Normalize_TiposSimples_LosDejaIgual(Object value) => TelemetryValue.Normalize(value, 10).Should().Be(value);

        [Fact]
        public void Normalize_UInt32_SeConvierteAInt64() => TelemetryValue.Normalize(7u, 10).Should().Be(7L);

        [Fact]
        public void Normalize_UInt64Pequeno_SeConvierteAInt64() => TelemetryValue.Normalize(7ul, 10).Should().Be(7L);

        [Fact]
        public void Normalize_UInt64Enorme_SeConvierteATexto() => TelemetryValue.Normalize(UInt64.MaxValue, 100).Should().Be(UInt64.MaxValue.ToString());

        [Fact]
        public void Normalize_Decimal_SeConvierteADouble() => TelemetryValue.Normalize(2.5m, 10).Should().Be(2.5d);

        [Fact]
        public void Normalize_FechaLocal_SeConvierteAUtc()
        {
            var local = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Local);

            var result = (DateTime)TelemetryValue.Normalize(local, 10)!;

            result.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Fact]
        public void Normalize_DateTimeOffset_SeConvierteAUtc()
        {
            var value = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.FromHours(-5));

            TelemetryValue.Normalize(value, 10).Should().Be(new DateTime(2026, 1, 1, 15, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public void Normalize_Guid_SeConvierteATexto()
        {
            Guid id = Guid.NewGuid();

            TelemetryValue.Normalize(id, 100).Should().Be(id.ToString());
        }

        [Fact]
        public void Normalize_TimeSpan_SeConvierteAMilisegundos() => TelemetryValue.Normalize(TimeSpan.FromSeconds(2), 10).Should().Be(2000d);

        [Fact]
        public void Normalize_Enum_SeConvierteATexto() => TelemetryValue.Normalize(Color.Rojo, 10).Should().Be("Rojo");

        [Fact]
        public void Normalize_ArregloPequeno_NormalizaCadaElemento()
        {
            var result = TelemetryValue.Normalize(new Object[] { 1, "abcdef", "xyz123" }, 3);

            result.Should().BeAssignableTo<IEnumerable<Object?>>()
                .Which.Should().Equal(1, "abc", "xyz");
        }

        [Fact]
        public void Normalize_ArregloGrande_SeConvierteATextoEnLugarDeRecorrerlo()
        {
            Int32[] big = Enumerable.Range(0, 100).ToArray();

            TelemetryValue.Normalize(big, 50).Should().BeOfType<String>();
        }

        [Fact]
        public void Normalize_ObjetoCualquiera_SeConvierteATextoTruncado()
        {
            TelemetryValue.Normalize(new Uri("http://localhost/muy/largo/camino"), 12).Should().Be("http://local");
        }

        [Fact]
        public void Truncate_ConMaximoCero_NoTrunca() => TelemetryValue.Truncate("abcdef", 0).Should().Be("abcdef");
    }
}
