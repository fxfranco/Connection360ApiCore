using Connection360Notification.Api.Middleware;
using Connection360Notification.Api.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace Connection360Notification.Api.Tests.Middleware
{
    public class MiddlewareCasosAdicionalesTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        private static DefaultHttpContext NewContext(String path = "/api/v1/test")
        {
            var context = new DefaultHttpContext();
            context.Request.Path = path;
            context.Response.Body = new MemoryStream();
            return context;
        }

        private static async Task<ApiResponse<Object>> ReadResponse(DefaultHttpContext context)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            String json = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<ApiResponse<Object>>(json, JsonOptions)!;
        }

        // ---------- ExceptionHandlingMiddleware ----------

        [Theory]
        [InlineData(typeof(ArgumentNullException), 400, "Solicitud inválida")]
        [InlineData(typeof(ArgumentOutOfRangeException), 400, "Solicitud inválida")]
        [InlineData(typeof(InvalidOperationException), 500, "Error interno del servidor")]
        [InlineData(typeof(NotSupportedException), 500, "Error interno del servidor")]
        [InlineData(typeof(OperationCanceledException), 500, "Error interno del servidor")]
        [InlineData(typeof(KeyNotFoundException), 404, "Recurso no encontrado")]
        [InlineData(typeof(UnauthorizedAccessException), 401, "No autorizado")]
        public async Task ExceptionHandling_MapeaCadaTipoDeExcepcionAlStatusYMensajeEsperado(Type exceptionType, Int32 expectedStatus, String expectedMessage)
        {
            var exception = (Exception)Activator.CreateInstance(exceptionType)!;
            var middleware = new ExceptionHandlingMiddleware(_ => throw exception, Mock.Of<ILogger<ExceptionHandlingMiddleware>>());
            DefaultHttpContext context = NewContext();

            await middleware.InvokeAsync(context);

            context.Response.StatusCode.Should().Be(expectedStatus);
            ApiResponse<Object> body = await ReadResponse(context);
            body.Status.Should().Be(expectedStatus);
            body.Message.Should().Be(expectedMessage);
            body.Error.Should().Be(exception.Message);
            body.DataResponse.Should().BeNull();
        }

        [Fact]
        public async Task ExceptionHandling_ConExcepcion_RegistraUnErrorEnElLogger()
        {
            var logger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
            logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            var exception = new InvalidOperationException("boom");
            var middleware = new ExceptionHandlingMiddleware(_ => throw exception, logger.Object);

            await middleware.InvokeAsync(NewContext("/api/v1/falla"));

            logger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, String>>()), Times.Once);
        }

        [Fact]
        public async Task ExceptionHandling_SinExcepcion_NoRegistraErrores()
        {
            var logger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
            var middleware = new ExceptionHandlingMiddleware(_ => Task.CompletedTask, logger.Object);

            await middleware.InvokeAsync(NewContext());

            logger.Verify(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, String>>()), Times.Never);
        }

        [Fact]
        public async Task ExceptionHandling_SinExcepcion_InvocaElSiguienteDelegadoUnaSolaVez()
        {
            Int32 calls = 0;
            var middleware = new ExceptionHandlingMiddleware(_ => { calls++; return Task.CompletedTask; }, Mock.Of<ILogger<ExceptionHandlingMiddleware>>());

            await middleware.InvokeAsync(NewContext());

            calls.Should().Be(1);
        }

        [Fact]
        public async Task ExceptionHandling_ConExcepcionAsincrona_TambienLaCaptura()
        {
            var middleware = new ExceptionHandlingMiddleware(async _ =>
            {
                await Task.Yield();
                throw new KeyNotFoundException("asincrona");
            }, Mock.Of<ILogger<ExceptionHandlingMiddleware>>());
            DefaultHttpContext context = NewContext();

            await middleware.InvokeAsync(context);

            context.Response.StatusCode.Should().Be(404);
            (await ReadResponse(context)).Error.Should().Be("asincrona");
        }

        [Fact]
        public async Task ExceptionHandling_ConExcepcion_GeneraTimestampReciente()
        {
            var middleware = new ExceptionHandlingMiddleware(_ => throw new Exception("x"), Mock.Of<ILogger<ExceptionHandlingMiddleware>>());
            DefaultHttpContext context = NewContext();
            DateTime before = DateTime.UtcNow.AddMinutes(-1);

            await middleware.InvokeAsync(context);

            (await ReadResponse(context)).Timestamp.ToUniversalTime().Should().BeAfter(before);
        }

        // ---------- NotFoundMiddleware ----------

        [Fact]
        public async Task NotFound_Con404_EscribeMensajeYErrorEstandarConElPath()
        {
            var middleware = new NotFoundMiddleware(ctx => { ctx.Response.StatusCode = 404; return Task.CompletedTask; });
            DefaultHttpContext context = NewContext("/ruta/inexistente");

            await middleware.InvokeAsync(context);

            context.Response.ContentType.Should().StartWith("application/json");
            ApiResponse<Object> body = await ReadResponse(context);
            body.Status.Should().Be(404);
            body.Message.Should().Be("Ruta no encontrada");
            body.Error.Should().Be("El recurso solicitado no existe");
            body.Path.Should().Be("/ruta/inexistente");
        }

        [Theory]
        [InlineData(200)]
        [InlineData(201)]
        [InlineData(204)]
        [InlineData(400)]
        [InlineData(401)]
        [InlineData(403)]
        [InlineData(500)]
        public async Task NotFound_ConStatusDistintoDe404_NoEscribeCuerpo(Int32 status)
        {
            var middleware = new NotFoundMiddleware(ctx => { ctx.Response.StatusCode = status; return Task.CompletedTask; });
            DefaultHttpContext context = NewContext();

            await middleware.InvokeAsync(context);

            context.Response.StatusCode.Should().Be(status);
            context.Response.Body.Length.Should().Be(0);
        }

        [Fact]
        public async Task NotFound_PropagaLasExcepcionesDelSiguienteMiddleware()
        {
            var middleware = new NotFoundMiddleware(_ => throw new InvalidOperationException("x"));

            Func<Task> act = () => middleware.InvokeAsync(NewContext());

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task NotFound_SinPath_EscribeElPathVacio()
        {
            var middleware = new NotFoundMiddleware(ctx => { ctx.Response.StatusCode = 404; return Task.CompletedTask; });
            DefaultHttpContext context = NewContext(path: String.Empty);

            await middleware.InvokeAsync(context);

            (await ReadResponse(context)).Path.Should().BeNullOrEmpty();
        }
    }
}
