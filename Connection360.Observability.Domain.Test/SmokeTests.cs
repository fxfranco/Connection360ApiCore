using Connection360.Observability.Domain.Models;
using FluentAssertions;
using Xunit;

namespace Connection360.Observability.Domain.Test
{
    public class SmokeTests
    {
        [Fact]
        public void ObservedServices_ExponeLosTresNombresDeAplicacion()
        {
            ObservedServices.ApiCore.Should().Be("ApiCore");
            ObservedServices.ApiNotification.Should().Be("ApiNotification");
            ObservedServices.Etl.Should().Be("Etl");
        }
    }
}
