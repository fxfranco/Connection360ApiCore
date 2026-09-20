using Connection360.Domain.Entities.Persistence;
using FluentAssertions;
using Xunit;

namespace Connection360.Domain.Tests.Entities.Persistence
{
    public class CustomerNotificationChannelsTests
    {
        [Fact]
        public void Constructor_DatosValidos_DebeAsignarTodasLasPropiedades()
        {
            var entity = new CustomerNotificationChannels(1, 10, true, false, true);

            entity.IdNotificationChannel.Should().Be(1);
            entity.IdCustomer.Should().Be(10);
            entity.Application.Should().BeTrue();
            entity.Email.Should().BeFalse();
            entity.TextMessages.Should().BeTrue();
        }

        [Fact]
        public void Constructor_IdCustomerCeroOMenor_DebeLanzarArgumentException()
        {
            Action act = () => new CustomerNotificationChannels(1, 0, true, true, true);

            act.Should().Throw<ArgumentException>().WithParameterName("idCustomer");
        }

        [Fact]
        public void Constructor_IdCustomerNegativo_DebeLanzarArgumentException()
        {
            Action act = () => new CustomerNotificationChannels(1, -5, true, true, true);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Update_DatosValidos_DebeActualizarPropiedades()
        {
            var entity = new CustomerNotificationChannels(1, 10, true, true, true);

            entity.Update(20, false, false, false);

            entity.IdCustomer.Should().Be(20);
            entity.Application.Should().BeFalse();
            entity.Email.Should().BeFalse();
            entity.TextMessages.Should().BeFalse();
        }

        [Fact]
        public void Update_IdCustomerInvalido_DebeLanzarArgumentExceptionYNoModificarEstado()
        {
            var entity = new CustomerNotificationChannels(1, 10, true, true, true);

            Action act = () => entity.Update(0, false, false, false);

            act.Should().Throw<ArgumentException>();
            entity.IdCustomer.Should().Be(10);
        }

        [Fact]
        public void Constructor_ConDatosValidos_AsignaTodasLasPropiedades()
        {
            var entity = new CustomerNotificationChannels(10, 5, true, false, true);

            entity.IdNotificationChannel.Should().Be(10);
            entity.IdCustomer.Should().Be(5);
            entity.Application.Should().BeTrue();
            entity.Email.Should().BeFalse();
            entity.TextMessages.Should().BeTrue();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_ConIdCustomerInvalido_LanzaArgumentException(Int64 idCustomerInvalido)
        {
            Action act = () => new CustomerNotificationChannels(1, idCustomerInvalido, true, true, true);

            act.Should().Throw<ArgumentException>()
                .WithParameterName("idCustomer");
        }

        [Fact]
        public void Update_ConDatosValidos_ActualizaLasPropiedades()
        {
            var entity = new CustomerNotificationChannels(1, 5, true, true, true);

            entity.Update(5, false, false, false);

            entity.Application.Should().BeFalse();
            entity.Email.Should().BeFalse();
            entity.TextMessages.Should().BeFalse();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void Update_ConIdCustomerInvalido_LanzaArgumentException(Int64 idCustomerInvalido)
        {
            var entity = new CustomerNotificationChannels(1, 5, true, true, true);

            Action act = () => entity.Update(idCustomerInvalido, true, true, true);

            act.Should().Throw<ArgumentException>()
                .WithParameterName("idCustomer");
        }

        [Fact]
        public void Update_NoModificaElIdNotificationChannel()
        {
            var entity = new CustomerNotificationChannels(99, 5, true, true, true);

            entity.Update(5, false, false, false);

            entity.IdNotificationChannel.Should().Be(99);
        }
    }
}
