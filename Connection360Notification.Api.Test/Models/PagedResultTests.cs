using Connection360Notification.Api.Models;
using FluentAssertions;
using Xunit;

namespace Connection360Notification.Api.Tests.Models
{
    public class PagedResultTests
    {
        [Fact]
        public void Items_PorDefecto_EsUnaColeccionVacia()
        {
            new PagedResult<String>().Items.Should().BeEmpty();
        }

        [Theory]
        [InlineData(100, 10, 10)]
        [InlineData(101, 10, 11)]
        [InlineData(1, 10, 1)]
        [InlineData(0, 10, 0)]
        public void TotalPages_CalculaCorrectamenteRedondeandoHaciaArriba(Int64 totalItems, Int64 limit, Int64 expected)
        {
            var result = new PagedResult<String> { TotalItems = totalItems, Limit = limit };

            result.TotalPages.Should().Be(expected);
        }

        [Fact]
        public void TotalPages_ConLimitCero_RetornaCeroYNoLanzaExcepcion()
        {
            var result = new PagedResult<String> { TotalItems = 50, Limit = 0 };

            result.TotalPages.Should().Be(0);
        }
    }
}
