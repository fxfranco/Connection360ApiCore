using Connection360.Api.Middleware;
using Connection360.Api.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace Connection360.Api.Tests.Middleware
{
    public class MiddlewareAdicionalesTests
    {
        private static DefaultHttpContext BuildContext(String path = "/api/v1/test")
        {
            var context = new DefaultHttpContext();
            context.Request.Path = path;
            context.Response.Body = new MemoryStream();
            return context;
        }

        private static ApiResponse<Object> Read(DefaultHttpContext context)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            return JsonSerializer.Deserialize<ApiResponse<Object>>(reader.ReadToEnd(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        [Fact]
        public async Task ExceptionHandling_ConExcepcion_RegistraElErrorEnElLogger()
        {
            var logger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
            logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            var middleware = new ExceptionHandlingMiddleware(_ => throw new InvalidOperationException("boom"), logger.Object);

            await middleware.InvokeAsync(BuildContext());

            logger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, String>>()), Times.Once);
        }

        [Theory]
        [InlineData(typeof(KeyNotFoundException), 404, "Recurso no encontrado")]
        [InlineData(typeof(UnauthorizedAccessException), 401, "No autorizado")]
        [InlineData(typeof(ArgumentException), 400, "Solicitud inválida")]
        [InlineData(typeof(ArgumentNullException), 400, "Solicitud inválida")]
        [InlineData(typeof(InvalidOperationException), 500, "Error interno del servidor")]
        [InlineData(typeof(NotSupportedException), 500, "Error interno del servidor")]
        public async Task ExceptionHandling_MapeaTipoDeExcepcionAStatusYMensaje(Type exceptionType, Int32 status, String message)
        {
            var exception = (Exception)Activator.CreateInstance(exceptionType, "detalle")!;
            var middleware = new ExceptionHandlingMiddleware(_ => throw exception, Mock.Of<ILogger<ExceptionHandlingMiddleware>>());
            DefaultHttpContext context = BuildContext("/api/v1/ruta");

            await middleware.InvokeAsync(context);

            context.Response.StatusCode.Should().Be(status);
            context.Response.ContentType.Should().StartWith("application/json");
            ApiResponse<Object> body = Read(context);
            body.Status.Should().Be(status);
            body.Message.Should().Be(message);
            body.Path.Should().Be("/api/v1/ruta");
            body.Error.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task ExceptionHandling_SinExcepcion_InvocaAlSiguienteUnaVez()
        {
            Int32 calls = 0;
            var middleware = new ExceptionHandlingMiddleware(_ => { calls++; return Task.CompletedTask; }, Mock.Of<ILogger<ExceptionHandlingMiddleware>>());

            await middleware.InvokeAsync(BuildContext());

            calls.Should().Be(1);
        }

        [Fact]
        public async Task NotFound_ConStatus404_IncluyeElPathEnLaRespuesta()
        {
            DefaultHttpContext context = BuildContext("/api/v1/inexistente");
            var middleware = new NotFoundMiddleware(c => { c.Response.StatusCode = StatusCodes.Status404NotFound; return Task.CompletedTask; });

            await middleware.InvokeAsync(context);

            context.Response.ContentType.Should().StartWith("application/json");
            Read(context).Path.Should().Be("/api/v1/inexistente");
        }

        [Theory]
        [InlineData(200)]
        [InlineData(400)]
        [InlineData(500)]
        public async Task NotFound_ConStatusDistintoDe404_NoEscribeCuerpo(Int32 status)
        {
            DefaultHttpContext context = BuildContext();
            var middleware = new NotFoundMiddleware(c => { c.Response.StatusCode = status; return Task.CompletedTask; });

            await middleware.InvokeAsync(context);

            context.Response.Body.Length.Should().Be(0);
            context.Response.StatusCode.Should().Be(status);
        }

        [Fact]
        public async Task NotFound_SiElSiguienteLanzaExcepcion_LaPropaga()
        {
            var middleware = new NotFoundMiddleware(_ => throw new InvalidOperationException("x"));

            Func<Task> act = () => middleware.InvokeAsync(BuildContext());

            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
