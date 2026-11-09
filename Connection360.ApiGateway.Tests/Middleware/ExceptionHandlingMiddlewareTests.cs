using Connection360.ApiGateway.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Xunit;

namespace Connection360.ApiGateway.Tests.Middleware
{
    public class ExceptionHandlingMiddlewareTests
    {
        private static DefaultHttpContext BuildContext()
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            return context;
        }

        [Fact]
        public async Task InvokeAsync_SinExcepcion_NoModificaLaRespuesta()
        {
            var context = BuildContext();
            var middleware = new ExceptionHandlingMiddleware(_ => Task.CompletedTask, NullLogger<ExceptionHandlingMiddleware>.Instance);

            await middleware.InvokeAsync(context);

            context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        [Fact]
        public async Task InvokeAsync_ConExcepcion_Retorna500ConCuerpoJsonSinDetallesInternos()
        {
            var context = BuildContext();
            var middleware = new ExceptionHandlingMiddleware(_ => throw new InvalidOperationException("detalle interno sensible"), NullLogger<ExceptionHandlingMiddleware>.Instance);

            await middleware.InvokeAsync(context);

            context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            context.Response.ContentType.Should().Be("application/json");

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            var body = await reader.ReadToEndAsync();

            body.Should().NotContain("detalle interno sensible");
            using var json = JsonDocument.Parse(body);
            json.RootElement.GetProperty("status").GetInt32().Should().Be(500);
            json.RootElement.GetProperty("title").GetString().Should().Be("Ha ocurrido un error inesperado.");
        }

        [Fact]
        public async Task InvokeAsync_ConExcepcion_IncluyeElTraceIdentifier()
        {
            var context = BuildContext();
            context.TraceIdentifier = "trace-123";
            var middleware = new ExceptionHandlingMiddleware(_ => throw new InvalidOperationException(), NullLogger<ExceptionHandlingMiddleware>.Instance);

            await middleware.InvokeAsync(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            var body = await reader.ReadToEndAsync();
            using var json = JsonDocument.Parse(body);

            json.RootElement.GetProperty("traceId").GetString().Should().Be("trace-123");
        }

        [Fact]
        public async Task InvokeAsync_ConExcepcion_NoRelanzaLaExcepcion()
        {
            var context = BuildContext();
            var middleware = new ExceptionHandlingMiddleware(_ => throw new InvalidOperationException("fallo"), NullLogger<ExceptionHandlingMiddleware>.Instance);

            Func<Task> act = () => middleware.InvokeAsync(context);

            await act.Should().NotThrowAsync();
        }
    }
}
