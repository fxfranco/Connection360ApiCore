using Connection360.Domain.Entities.Persistence;
using FluentAssertions;
using Xunit;

namespace Connection360.Domain.Tests.Entities.Persistence
{
    public class CustomerNotificationEventsTests
    {
        [Fact]
        public void Constructor_DatosValidos_DebeAsignarTodasLasPropiedades()
        {
            var entity = new CustomerNotificationEvents(1, 10, true, false, true, false, true);

            entity.IdNotificationEvent.Should().Be(1);
            entity.IdCustomer.Should().Be(10);
            entity.ChangeState.Should().BeTrue();
            entity.SuccessfulDelivery.Should().BeFalse();
            entity.WithIssues.Should().BeTrue();
            entity.ShipmentTransit.Should().BeFalse();
            entity.DeliveryReminder.Should().BeTrue();
        }

        [Fact]
        public void Constructor_IdCustomerInvalido_DebeLanzarArgumentException()
        {
            Action act = () => new CustomerNotificationEvents(1, 0, true, true, true, true, true);

            act.Should().Throw<ArgumentException>().WithParameterName("idCustomer");
        }

        [Fact]
        public void Update_DatosValidos_DebeActualizarTodasLasPropiedades()
        {
            var entity = new CustomerNotificationEvents(1, 10, true, true, true, true, true);

            entity.Update(30, false, false, false, false, false);

            entity.IdCustomer.Should().Be(30);
            entity.ChangeState.Should().BeFalse();
            entity.SuccessfulDelivery.Should().BeFalse();
            entity.WithIssues.Should().BeFalse();
            entity.ShipmentTransit.Should().BeFalse();
            entity.DeliveryReminder.Should().BeFalse();
        }

        [Fact]
        public void Update_IdCustomerInvalido_DebeLanzarArgumentException()
        {
            var entity = new CustomerNotificationEvents(1, 10, true, true, true, true, true);

            Action act = () => entity.Update(-1, true, true, true, true, true);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_ConDatosValidos_AsignaTodasLasPropiedades()
        {
            var entity = new CustomerNotificationEvents(10, 5, true, true, false, true, false);

            entity.IdNotificationEvent.Should().Be(10);
            entity.IdCustomer.Should().Be(5);
            entity.ChangeState.Should().BeTrue();
            entity.SuccessfulDelivery.Should().BeTrue();
            entity.WithIssues.Should().BeFalse();
            entity.ShipmentTransit.Should().BeTrue();
            entity.DeliveryReminder.Should().BeFalse();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_ConIdCustomerInvalido_LanzaArgumentException(Int64 idCustomerInvalido)
        {
            Action act = () => new CustomerNotificationEvents(1, idCustomerInvalido, true, true, true, true, true);

            act.Should().Throw<ArgumentException>()
                .WithParameterName("idCustomer");
        }

        [Fact]
        public void Update_ConDatosValidos_ActualizaLasPropiedades()
        {
            var entity = new CustomerNotificationEvents(1, 5, true, true, true, true, true);

            entity.Update(5, false, false, false, false, false);

            entity.ChangeState.Should().BeFalse();
            entity.SuccessfulDelivery.Should().BeFalse();
            entity.WithIssues.Should().BeFalse();
            entity.ShipmentTransit.Should().BeFalse();
            entity.DeliveryReminder.Should().BeFalse();
        }

        [Fact]
        public void Update_ConIdCustomerInvalido_LanzaArgumentException()
        {
            var entity = new CustomerNotificationEvents(1, 5, true, true, true, true, true);

            Action act = () => entity.Update(0, true, true, true, true, true);

            act.Should().Throw<ArgumentException>()
                .WithParameterName("idCustomer");
        }
    }
}
