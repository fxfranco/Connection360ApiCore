using System.Reflection;
using Connection360.Api.Controllers;
using Connection360.Api.Tests.TestSupport;
using Connection360.Application.DTOs;
using Connection360.Application.DTOs.Persistence;
using Connection360.Application.Ports;
using Connection360.Application.Ports.Persistence;
using Connection360.Domain.Dtos;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;
using Xunit;

namespace Connection360.Api.Tests.Controllers
{
    /// <summary>
    /// Los CreatedAtRoute del controlador solo generan el encabezado Location si la ruta destino tiene nombre.
    /// </summary>
    public class SettingsControllerRoutingTests
    {
        private static String? RouteNameOf(String action)
        {
            MethodInfo method = typeof(SettingsController).GetMethod(action)!;
            return method.GetCustomAttributes().OfType<HttpMethodAttribute>().Single().Name;
        }

        [Theory]
        [InlineData(nameof(SettingsController.GetNotificationsSettings))]
        [InlineData(nameof(SettingsController.GetMasterSettings))]
        [InlineData(nameof(SettingsController.CreateCustomerDataBase))]
        [InlineData(nameof(SettingsController.CreateCollaboratorDataBase))]
        [InlineData(nameof(SettingsController.CreateCustomerCollaboratorDataBase))]
        public void LasRutasUsadasPorCreatedAtRoute_TienenElNombreDeLaAccion(String action)
        {
            RouteNameOf(action).Should().Be(action);
        }

        [Fact]
        public void LosNombresDeRutaDelControlador_SonUnicos()
        {
            var names = typeof(SettingsController).GetMethods()
                .SelectMany(m => m.GetCustomAttributes().OfType<HttpMethodAttribute>())
                .Select(a => a.Name)
                .Where(n => n != null)
                .ToList();

            names.Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public async Task CreateNotificationsSettings_IncluyeElClienteEnLosValoresDeRutaParaPoderConsultarlo()
        {
            var useCase = new Mock<ICustomerNotificationsSettingsUseCase>();
            var created = new CustomerNotificationsSettingsResponse
            {
                NotificationChannels = new NotificationChannelsResponse { ClientId = "CLI-1", NotificationChannelId = 7 },
                NotificationEvents = new NotificationEventsResponse { ClientId = "CLI-1", NotificationEventId = 9 }
            };
            useCase.Setup(u => u.CreateCustomerNotificationSettings(It.IsAny<CustomerNotificationsSettingsResponse>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(created);
            var sut = new SettingsController(Mock.Of<IGetUserManagementUseCase>(), Mock.Of<ICustomerUseCase>(), useCase.Object,
                Mock.Of<IMasterSettingsUseCase>(), Mock.Of<ICollaboratorUseCase>(), Mock.Of<IOutboxMessagesUseCase>())
            {
                ControllerContext = ControllerContextFactory.Create("CLIENT")
            };

            IActionResult result = await sut.CreateNotificationsSettings(new CustomerNotificationsSettingsResponse(), CancellationToken.None);

            var route = result.Should().BeOfType<CreatedAtRouteResult>().Subject;
            route.RouteName.Should().Be(nameof(SettingsController.GetNotificationsSettings));
            route.RouteValues!["idClient"].Should().Be("CLI-1");
            route.RouteValues["idChannel"].Should().Be(7L);
            route.RouteValues["idEvent"].Should().Be(9L);
        }

        [Fact]
        public async Task CreateNotificationsSettings_SiElClienteSoloVieneEnEventos_LoTomaDeEventos()
        {
            var useCase = new Mock<ICustomerNotificationsSettingsUseCase>();
            useCase.Setup(u => u.CreateCustomerNotificationSettings(It.IsAny<CustomerNotificationsSettingsResponse>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CustomerNotificationsSettingsResponse
                {
                    NotificationChannels = new NotificationChannelsResponse { ClientId = null!, NotificationChannelId = 1 },
                    NotificationEvents = new NotificationEventsResponse { ClientId = "CLI-2", NotificationEventId = 2 }
                });
            var sut = new SettingsController(Mock.Of<IGetUserManagementUseCase>(), Mock.Of<ICustomerUseCase>(), useCase.Object,
                Mock.Of<IMasterSettingsUseCase>(), Mock.Of<ICollaboratorUseCase>(), Mock.Of<IOutboxMessagesUseCase>())
            {
                ControllerContext = ControllerContextFactory.Create("ADMIN")
            };

            IActionResult result = await sut.CreateNotificationsSettings(new CustomerNotificationsSettingsResponse(), CancellationToken.None);

            result.Should().BeOfType<CreatedAtRouteResult>().Which.RouteValues!["idClient"].Should().Be("CLI-2");
        }
    }
}
