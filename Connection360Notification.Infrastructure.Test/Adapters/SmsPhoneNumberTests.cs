using Connection360Notification.Infrastructure.Sms;
using FluentAssertions;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Adapters
{
    public class SmsPhoneNumberTests
    {
        [Theory]
        [InlineData("3113232323", "57", "573113232323")]
        [InlineData(" 311 323 2323 ", "57", "573113232323")]
        [InlineData("(311) 323-2323", "57", "573113232323")]
        [InlineData("311.323.2323", "57", "573113232323")]
        [InlineData("+573113232323", "57", "573113232323")]
        [InlineData("+57 311 323 2323", "1", "573113232323")]
        [InlineData("573113232323", "57", "573113232323")]
        [InlineData("00573113232323", "57", "573113232323")]
        [InlineData("+34612345678", "57", "34612345678")]
        [InlineData("3113232323", "+57", "573113232323")]
        public void Normalize_DejaIndicativoMasNumeroSoloConDigitos(String phone, String country, String esperado)
        {
            SmsPhoneNumber.Normalize(phone, country).Should().Be(esperado);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("abc")]
        [InlineData("311-ABC-2323")]
        [InlineData("+")]
        [InlineData("+1234567")]
        [InlineData("+1234567890123456")]
        public void Normalize_ConValorVacioInvalidoOFueraDeRango_RetornaNull(String? phone)
        {
            SmsPhoneNumber.Normalize(phone, "57").Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Normalize_SinIndicativoPorDefecto_ValidaElNumeroTalCual(String? country)
        {
            SmsPhoneNumber.Normalize("3113232323", country).Should().Be("3113232323");
            SmsPhoneNumber.Normalize("1234567", country).Should().BeNull();
        }
    }
}
