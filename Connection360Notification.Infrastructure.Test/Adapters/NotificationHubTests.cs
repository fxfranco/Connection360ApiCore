using System.Reflection;
using Connection360Notification.Infrastructure.Adapters.Input;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Adapters
{
    public class NotificationHubTests
    {
        [Fact]
        public void NotificationHub_EsUnHubDeSignalR()
        {
            typeof(NotificationHub).Should().BeAssignableTo<Hub>();
        }

        [Fact]
        public void NotificationHub_RequiereAutorizacionConLosRolesPermitidos()
        {
            var atributo = typeof(NotificationHub).GetCustomAttribute<AuthorizeAttribute>();

            atributo.Should().NotBeNull();
            atributo!.Roles!.Split(',').Should().BeEquivalentTo("ADMIN", "CLIENT", "ANALISTAOPE", "ANALISTASAC");
        }

        [Fact]
        public async Task OnConnectedAsync_ConUsuarioIdentificado_CompletaSinError()
        {
            var contexto = new Mock<HubCallerContext>();
            contexto.SetupGet(c => c.UserIdentifier).Returns("CLI-1");
            using var hub = new NotificationHub { Context = contexto.Object };

            Func<Task> act = () => hub.OnConnectedAsync();

            await act.Should().NotThrowAsync();
            contexto.VerifyGet(c => c.UserIdentifier, Times.Once);
        }

        [Fact]
        public async Task OnConnectedAsync_SinUsuarioIdentificado_CompletaSinError()
        {
            var contexto = new Mock<HubCallerContext>();
            contexto.SetupGet(c => c.UserIdentifier).Returns((String?)null);
            using var hub = new NotificationHub { Context = contexto.Object };

            Func<Task> act = () => hub.OnConnectedAsync();

            await act.Should().NotThrowAsync();
        }
    }
}
