using FluentAssertions;
using Xunit;

namespace Connection360Notification.Domain.Tests
{
    public class BasicUserRolesTests
    {
        [Fact]
        public void Admin_EsElNombreDelRolAdministrador()
        {
            BasicUserRoles.Admin.Should().Be("ADMIN");
        }
    }
}
