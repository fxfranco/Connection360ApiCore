using Connection360Notification.Api.Common;
using Connection360Notification.Api.Models;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace Connection360Notification.Api.Tests.Models
{
    public class ModelosApiTests
    {
        [Fact]
        public void ApiResponse_PorDefecto_TieneValoresInicialesEsperados()
        {
            DateTime before = DateTime.UtcNow.AddSeconds(-5);

            var response = new ApiResponse<String>();

            response.Status.Should().Be(0);
            response.Error.Should().BeNull();
            response.Message.Should().BeNull();
            response.DataResponse.Should().BeNull();
            response.Meta.Should().BeNull();
            response.Path.Should().BeNull();
            response.Timestamp.Should().BeAfter(before);
            response.Timestamp.Should().BeOnOrBefore(DateTime.UtcNow.AddSeconds(5));
        }

        [Fact]
        public void ApiResponse_ConPropiedadesAsignadas_LasConserva()
        {
            var meta = new MetaResponse { TotalItems = 1 };
            var timestamp = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);

            var response = new ApiResponse<Int32>
            {
                Timestamp = timestamp,
                Status = 201,
                Error = "e",
                Message = "m",
                DataResponse = 7,
                Meta = meta,
                Path = "/p"
            };

            response.Timestamp.Should().Be(timestamp);
            response.Status.Should().Be(201);
            response.Error.Should().Be("e");
            response.Message.Should().Be("m");
            response.DataResponse.Should().Be(7);
            response.Meta.Should().BeSameAs(meta);
            response.Path.Should().Be("/p");
        }

        [Fact]
        public void ApiResponse_SerializadaAJson_UsaLosNombresDePropiedadEsperados()
        {
            var response = new ApiResponse<String> { Status = 200, Message = "ok", DataResponse = "dato", Path = "/x" };

            String json = JsonSerializer.Serialize(response);

            using JsonDocument doc = JsonDocument.Parse(json);
            doc.RootElement.TryGetProperty("Status", out _).Should().BeTrue();
            doc.RootElement.GetProperty("DataResponse").GetString().Should().Be("dato");
            doc.RootElement.GetProperty("Message").GetString().Should().Be("ok");
            doc.RootElement.GetProperty("Path").GetString().Should().Be("/x");
        }

        [Fact]
        public void MetaResponse_PorDefecto_TieneTodosLosValoresEnCero()
        {
            var meta = new MetaResponse();

            meta.TotalItems.Should().Be(0);
            meta.TotalPages.Should().Be(0);
            meta.CurrentPage.Should().Be(0);
            meta.Limit.Should().Be(0);
        }

        [Fact]
        public void MetaResponse_ConPropiedadesAsignadas_LasConserva()
        {
            var meta = new MetaResponse { TotalItems = 100, TotalPages = 10, CurrentPage = 3, Limit = 10 };

            meta.TotalItems.Should().Be(100);
            meta.TotalPages.Should().Be(10);
            meta.CurrentPage.Should().Be(3);
            meta.Limit.Should().Be(10);
        }

        [Fact]
        public void PagedResult_ConPropiedadesAsignadas_LasConserva()
        {
            var paged = new PagedResult<String> { Items = new[] { "a" }, TotalItems = 5, CurrentPage = 2, Limit = 2 };

            paged.Items.Should().ContainSingle().Which.Should().Be("a");
            paged.TotalItems.Should().Be(5);
            paged.CurrentPage.Should().Be(2);
            paged.Limit.Should().Be(2);
            paged.TotalPages.Should().Be(3);
        }

        [Fact]
        public void PagedResult_ConLimitNegativo_RetornaCeroPaginas()
        {
            var paged = new PagedResult<String> { TotalItems = 10, Limit = -1 };

            paged.TotalPages.Should().Be(0);
        }

        [Fact]
        public void ApiResponseFactory_Success_ConMetaYTipoComplejo_LoConserva()
        {
            var meta = new MetaResponse { TotalItems = 2, TotalPages = 1, CurrentPage = 1, Limit = 10 };
            var data = new List<String> { "a", "b" };

            ApiResponse<List<String>> response = ApiResponseFactory.Success(data, "ok", 200, "/p", meta);

            response.DataResponse.Should().BeSameAs(data);
            response.Meta.Should().BeSameAs(meta);
            response.Error.Should().BeNull();
            response.Message.Should().Be("ok");
            response.Status.Should().Be(200);
            response.Path.Should().Be("/p");
        }

        [Fact]
        public void ApiResponseFactory_Fail_SinMensaje_UsaElMensajePorDefectoYSinData()
        {
            ApiResponse<Object> response = ApiResponseFactory.Fail("boom", 500, "/err");

            response.Message.Should().Be("Ocurrió un error al procesar la solicitud");
            response.Error.Should().Be("boom");
            response.Status.Should().Be(500);
            response.Path.Should().Be("/err");
            response.DataResponse.Should().BeNull();
            response.Meta.Should().BeNull();
        }
    }
}
