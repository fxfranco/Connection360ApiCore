using Connection360Notification.Api.Common;
using Connection360Notification.Api.Models;
using FluentAssertions;
using Xunit;

namespace Connection360Notification.Api.Tests.Common
{
    public class ApiResponseFactoryTests
    {
        [Fact]
        public void Success_ConstruyeLaRespuestaConLosDatosYMensajeProvistos()
        {
            var meta = new MetaResponse { TotalItems = 5 };

            ApiResponse<String> result = ApiResponseFactory.Success("data", "OK", 200, "/api/v1/test", meta);

            result.Status.Should().Be(200);
            result.Message.Should().Be("OK");
            result.DataResponse.Should().Be("data");
            result.Path.Should().Be("/api/v1/test");
            result.Meta.Should().BeSameAs(meta);
        }

        [Fact]
        public void Success_SinMeta_DejaMetaEnNull()
        {
            ApiResponse<String> result = ApiResponseFactory.Success("data", "OK", 200, "/api/v1/test");

            result.Meta.Should().BeNull();
        }

        [Fact]
        public void Fail_ConstruyeLaRespuestaConElErrorProvisto()
        {
            ApiResponse<Object> result = ApiResponseFactory.Fail("Bad input", 400, "/api/v1/test");

            result.Status.Should().Be(400);
            result.Error.Should().Be("Bad input");
            result.DataResponse.Should().BeNull();
            result.Message.Should().Be("Ocurrió un error al procesar la solicitud");
        }

        [Fact]
        public void Fail_ConMensajePersonalizado_LoUsaEnLugarDelPorDefecto()
        {
            ApiResponse<Object> result = ApiResponseFactory.Fail("Bad input", 400, "/api/v1/test", "Mensaje custom");

            result.Message.Should().Be("Mensaje custom");
        }
    }
}
