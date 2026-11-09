using Connection360Notification.Domain.Enums;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace Connection360Notification.Domain.Tests
{
    public class NotificationChannelPreferencesTests
    {
        [Fact]
        public void PorDefecto_NingunCanalEstaHabilitado()
        {
            var sut = new NotificationChannelPreferences();

            sut.Application.Should().BeFalse();
            sut.Email.Should().BeFalse();
            sut.TextMessages.Should().BeFalse();
            sut.HasAnyEnabled.Should().BeFalse();
        }

        [Fact]
        public void GetEnabledChannels_SinNingunCanalHabilitado_DevuelveSoloElCanalDeAplicacionPorDefecto()
        {
            new NotificationChannelPreferences().GetEnabledChannels().Should().Equal(NotificationChannelType.Application);
        }

        [Theory]
        [InlineData(true, false, false, new[] { NotificationChannelType.Application })]
        [InlineData(false, true, false, new[] { NotificationChannelType.Email })]
        [InlineData(false, false, true, new[] { NotificationChannelType.TextMessages })]
        [InlineData(true, true, false, new[] { NotificationChannelType.Application, NotificationChannelType.Email })]
        [InlineData(false, true, true, new[] { NotificationChannelType.Email, NotificationChannelType.TextMessages })]
        [InlineData(true, false, true, new[] { NotificationChannelType.Application, NotificationChannelType.TextMessages })]
        [InlineData(true, true, true, new[] { NotificationChannelType.Application, NotificationChannelType.Email, NotificationChannelType.TextMessages })]
        public void GetEnabledChannels_DevuelveExactamenteLosCanalesHabilitados(Boolean app, Boolean email, Boolean sms, NotificationChannelType[] esperados)
        {
            var sut = new NotificationChannelPreferences { Application = app, Email = email, TextMessages = sms };

            sut.HasAnyEnabled.Should().BeTrue();
            sut.GetEnabledChannels().Should().Equal(esperados);
        }

        [Fact]
        public void GetEnabledChannels_SoloEmailHabilitado_NoAgregaElCanalDeAplicacion()
        {
            var sut = new NotificationChannelPreferences { Email = true };

            sut.GetEnabledChannels().Should().NotContain(NotificationChannelType.Application);
        }

        [Theory]
        [InlineData(NotificationChannelType.Application)]
        [InlineData(NotificationChannelType.Email)]
        [InlineData(NotificationChannelType.TextMessages)]
        public void IsEnabled_ReflejaElIndicadorDeCadaCanal(NotificationChannelType canal)
        {
            var sut = new NotificationChannelPreferences
            {
                Application = canal == NotificationChannelType.Application,
                Email = canal == NotificationChannelType.Email,
                TextMessages = canal == NotificationChannelType.TextMessages
            };

            foreach (NotificationChannelType otro in Enum.GetValues<NotificationChannelType>())
                sut.IsEnabled(otro).Should().Be(otro == canal);
        }

        [Fact]
        public void IsEnabled_ConValorFueraDelEnum_DevuelveFalse()
        {
            new NotificationChannelPreferences { Application = true, Email = true, TextMessages = true }
                .IsEnabled((NotificationChannelType)99).Should().BeFalse();
        }

        [Fact]
        public void NotificationChannelType_TieneUnValorPorCadaCanalEsperado()
        {
            Enum.GetValues<NotificationChannelType>().Should().Equal(
                NotificationChannelType.Application, NotificationChannelType.Email, NotificationChannelType.TextMessages);
            ((Int32)NotificationChannelType.Application).Should().Be(0, "es el canal por defecto");
        }

        [Fact]
        public void EnCadaValorDelEnum_ExisteUnIndicadorEnLasPreferencias()
        {
            // Protege contra agregar un canal al enum y olvidar su indicador/IsEnabled.
            var todas = new NotificationChannelPreferences { Application = true, Email = true, TextMessages = true };

            foreach (NotificationChannelType canal in Enum.GetValues<NotificationChannelType>())
                todas.IsEnabled(canal).Should().BeTrue($"{canal} debe tener su indicador en NotificationChannelPreferences");
            todas.GetEnabledChannels().Should().BeEquivalentTo(Enum.GetValues<NotificationChannelType>());
        }

        [Fact]
        public void Deserializacion_ConElContratoDelPayload_CapturaLosTresIndicadores()
        {
            var sut = JsonSerializer.Deserialize<NotificationChannelPreferences>("{\"Application\":true,\"Email\":false,\"TextMessages\":true}");

            sut!.Application.Should().BeTrue();
            sut.Email.Should().BeFalse();
            sut.TextMessages.Should().BeTrue();
        }
    }
}
