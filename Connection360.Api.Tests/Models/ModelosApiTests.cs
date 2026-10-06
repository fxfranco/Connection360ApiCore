using Connection360.Api.Models;
using FluentAssertions;
using Xunit;

namespace Connection360.Api.Tests.Models
{
    public class ModelosApiTests
    {
        [Fact]
        public void ApiResponse_PorDefecto_TimestampEsRecienteYDemasCamposNulos()
        {
            DateTime before = DateTime.UtcNow.AddSeconds(-5);

            var response = new ApiResponse<String>();

            response.Timestamp.Should().BeOnOrAfter(before);
            response.Timestamp.Should().BeOnOrBefore(DateTime.UtcNow.AddSeconds(5));
            response.Status.Should().Be(0);
            response.Error.Should().BeNull();
            response.Message.Should().BeNull();
            response.DataResponse.Should().BeNull();
            response.Meta.Should().BeNull();
            response.Path.Should().BeNull();
        }

        [Fact]
        public void ApiResponse_AsignaTodasLasPropiedades()
        {
            var meta = new MetaResponse { TotalItems = 10, TotalPages = 2, CurrentPage = 1, Limit = 5 };
            var timestamp = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);

            var response = new ApiResponse<Int32>
            {
                Timestamp = timestamp, Status = 201, Error = "e", Message = "m", DataResponse = 7, Meta = meta, Path = "/p"
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
        public void MetaResponse_AsignaYLeeTodasLasPropiedades()
        {
            var meta = new MetaResponse { TotalItems = 100, TotalPages = 10, CurrentPage = 3, Limit = 10 };

            meta.TotalItems.Should().Be(100);
            meta.TotalPages.Should().Be(10);
            meta.CurrentPage.Should().Be(3);
            meta.Limit.Should().Be(10);
        }

        [Fact]
        public void MetaResponse_PorDefecto_TodoEnCero()
        {
            var meta = new MetaResponse();

            meta.TotalItems.Should().Be(0);
            meta.TotalPages.Should().Be(0);
            meta.CurrentPage.Should().Be(0);
            meta.Limit.Should().Be(0);
        }

        [Fact]
        public void PagedResult_AsignaPropiedades()
        {
            var paged = new PagedResult<String> { Items = ["a"], TotalItems = 11, CurrentPage = 2, Limit = 5 };

            paged.Items.Should().Equal("a");
            paged.TotalItems.Should().Be(11);
            paged.CurrentPage.Should().Be(2);
            paged.Limit.Should().Be(5);
            paged.TotalPages.Should().Be(3);
        }

        [Fact]
        public void PagedResult_ConLimiteNegativo_TotalPagesEsCero()
        {
            var paged = new PagedResult<String> { TotalItems = 10, Limit = -1 };

            paged.TotalPages.Should().Be(0);
        }
    }
}
