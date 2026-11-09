using Connection360.ApiGateway.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Connection360.ApiGateway.Tests.Middleware
{
    public class RequestLoggingMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_SinExcepcion_InvocaElSiguienteMiddlewareYPropagaElStatusCode()
        {
            var context = new DefaultHttpContext();
            var middleware = new RequestLoggingMiddleware(ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status201Created;
                return Task.CompletedTask;
            }, NullLogger<RequestLoggingMiddleware>.Instance);

            await middleware.InvokeAsync(context);

            context.Response.StatusCode.Should().Be(StatusCodes.Status201Created);
        }

        [Fact]
        public async Task InvokeAsync_SiLaRespuestaNoHaComenzadoYFalla_RelanzaLaExcepcion()
        {
            var context = new DefaultHttpContext();
            var middleware = new RequestLoggingMiddleware(_ => throw new InvalidOperationException("fallo antes de responder"), NullLogger<RequestLoggingMiddleware>.Instance);

            Func<Task> act = () => middleware.InvokeAsync(context);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("fallo antes de responder");
        }
    }
}
