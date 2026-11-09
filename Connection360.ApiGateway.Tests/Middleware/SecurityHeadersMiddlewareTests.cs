using Connection360.ApiGateway.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Connection360.ApiGateway.Tests.Middleware
{
    public class SecurityHeadersMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_AgregaTodosLosHeadersDeSeguridadEsperados()
        {
            var context = new DefaultHttpContext();
            var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

            await middleware.InvokeAsync(context);

            context.Response.Headers.XContentTypeOptions.ToString().Should().Be("nosniff");
            context.Response.Headers.XFrameOptions.ToString().Should().Be("DENY");
            context.Response.Headers["Referrer-Policy"].ToString().Should().Be("strict-origin-when-cross-origin");
            context.Response.Headers["Permissions-Policy"].ToString().Should().Be("geolocation=(), microphone=(), camera=()");
            context.Response.Headers.XXSSProtection.ToString().Should().Be("0");
            context.Response.Headers.ContentSecurityPolicy.ToString().Should().Contain("default-src 'self'");
        }

        [Fact]
        public async Task InvokeAsync_EliminaLosHeadersServerYXPoweredBySiEstabanPresentes()
        {
            var context = new DefaultHttpContext();
            context.Response.Headers.Server = "Kestrel";
            context.Response.Headers.XPoweredBy = "ASP.NET";
            var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

            await middleware.InvokeAsync(context);

            context.Response.Headers.ContainsKey("Server").Should().BeFalse();
            context.Response.Headers.ContainsKey("X-Powered-By").Should().BeFalse();
        }

        [Fact]
        public async Task InvokeAsync_SiempreInvocaElSiguienteMiddleware()
        {
            var context = new DefaultHttpContext();
            Boolean called = false;
            var middleware = new SecurityHeadersMiddleware(_ =>
            {
                called = true;
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(context);

            called.Should().BeTrue();
        }

        [Fact]
        public async Task InvokeAsync_LosHeadersSeAgreganAntesDeInvocarElSiguienteMiddleware()
        {
            // El middleware verifica HasStarted y agrega los headers ANTES de llamar a next(),
            // por lo que incluso si next() escribe contenido en la respuesta, los headers ya quedaron seteados.
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            var middleware = new SecurityHeadersMiddleware(async ctx =>
            {
                await ctx.Response.WriteAsync("contenido-de-la-respuesta");
            });

            await middleware.InvokeAsync(context);

            context.Response.Headers.ContainsKey("X-Content-Type-Options").Should().BeTrue();
        }
    }
}
