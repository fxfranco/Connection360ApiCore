using Connection360Notification.Infrastructure.Adapters.Input;
using FluentAssertions;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using System.IO.Pipelines;
using Xunit;

namespace Connection360Notification.Infrastructure.Tests.Adapters
{
    /// <summary>
    /// Fake mínimo de ConnectionContext que expone una colección de Features editable,
    /// necesaria para inyectar el IHttpContextFeature que SignalR usa internamente
    /// en HubConnectionContext.GetHttpContext().
    /// </summary>
    internal sealed class FakeConnectionContext : ConnectionContext
    {
        public override IDuplexPipe Transport { get; set; } = null!;
        //public override IDuplexPipe Transport { set => throw new NotImplementedException(); }
        public override String ConnectionId { get; set; } = "test-connection";
        public override IFeatureCollection Features { get; } = new FeatureCollection();
        public override IDictionary<Object, Object?> Items { get; set; } = new Dictionary<Object, Object?>();
    }

    internal sealed class FakeHttpContextFeature : IHttpContextFeature
    {
        public HttpContext? HttpContext { get; set; }
    }

    public class CustomUserIdProviderTests
    {
        private readonly CustomUserIdProvider _sut = new();

        private static HubConnectionContext BuildContext(HttpContext? httpContext)
        {
            var connection = new FakeConnectionContext();
            if (httpContext is not null)
            {
                connection.Features.Set<IHttpContextFeature>(new FakeHttpContextFeature { HttpContext = httpContext });
            }

            var options = new HubConnectionContextOptions();
            return new HubConnectionContext(connection, options, NullLoggerFactory.Instance);
        }

        [Fact]
        public void GetUserId_ConIdClientEnQueryString_RetornaElValor()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString("?idClient=CLIENTE-123");
            var connectionContext = BuildContext(httpContext);

            String? result = _sut.GetUserId(connectionContext);

            result.Should().Be("CLIENTE-123");
        }

        [Fact]
        public void GetUserId_SinIdClientEnQueryString_RetornaNull()
        {
            var httpContext = new DefaultHttpContext();
            var connectionContext = BuildContext(httpContext);

            String? result = _sut.GetUserId(connectionContext);

            result.Should().BeNull();
        }

        [Fact]
        public void GetUserId_ConIdClientVacio_RetornaNull()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString("?idClient=");
            var connectionContext = BuildContext(httpContext);

            String? result = _sut.GetUserId(connectionContext);

            result.Should().BeNull();
        }

        [Fact]
        public void GetUserId_SinHttpContextDisponible_RetornaNull()
        {
            var connectionContext = BuildContext(httpContext: null);

            String? result = _sut.GetUserId(connectionContext);

            result.Should().BeNull();
        }
    }
}
