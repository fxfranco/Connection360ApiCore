using Connection360.Domain.Entities.Persistence;
using FluentAssertions;
using Xunit;

namespace Connection360.Domain.Tests.Entities.Persistence
{
    public class CollaboratorNotificationChannelsTests
    {
        [Fact]
        public void Constructor_DatosValidos_AsignaTodasLasPropiedades()
        {
            var entity = new CollaboratorNotificationChannels(1, 10, true, false, true);

            entity.IdNotificationChannel.Should().Be(1);
            entity.IdCollaborator.Should().Be(10);
            entity.Application.Should().BeTrue();
            entity.Email.Should().BeFalse();
            entity.TextMessages.Should().BeTrue();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void Constructor_IdColaboradorCeroONegativo_LanzaArgumentException(Int64 idCollaborator)
        {
            Action act = () => _ = new CollaboratorNotificationChannels(1, idCollaborator, true, true, true);

            act.Should().Throw<ArgumentException>().WithParameterName(nameof(idCollaborator));
        }

        [Fact]
        public void Update_DatosValidos_ActualizaLasPropiedadesSinCambiarElIdDelRegistro()
        {
            var entity = new CollaboratorNotificationChannels(1, 10, true, true, true);

            entity.Update(20, false, false, false);

            entity.IdNotificationChannel.Should().Be(1);
            entity.IdCollaborator.Should().Be(20);
            entity.Application.Should().BeFalse();
            entity.Email.Should().BeFalse();
            entity.TextMessages.Should().BeFalse();
        }

        [Fact]
        public void Update_IdColaboradorInvalido_LanzaArgumentExceptionYNoModificaElEstado()
        {
            var entity = new CollaboratorNotificationChannels(1, 10, true, true, true);

            Action act = () => entity.Update(0, false, false, false);

            act.Should().Throw<ArgumentException>();
            entity.IdCollaborator.Should().Be(10);
            entity.Application.Should().BeTrue();
        }
    }
}
