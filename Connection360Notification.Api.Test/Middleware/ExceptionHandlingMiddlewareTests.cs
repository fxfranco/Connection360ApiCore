using Connection360Notification.Api.Middleware;
using Connection360Notification.Api.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Xunit;

namespace Connection360Notification.Api.Tests.Middleware
{
    public class ExceptionHandlingMiddlewareTests
    {
        private static (ExceptionHandlingMiddleware Middleware, DefaultHttpContext Context) Build(RequestDelegate next)
        {
            var middleware = new ExceptionHandlingMiddleware(next, NullLogger<ExceptionHandlingMiddleware>.Instance);
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/v1/test";
            context.Response.Body = new MemoryStream();
            return (middleware, context);
        }

        private static async Task<ApiResponse<Object>> ReadResponse(DefaultHttpContext context)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            var json = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<ApiResponse<Object>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        [Fact]
        public async Task InvokeAsync_SinExcepcion_NoModificaLaRespuesta()
        {
            var (middleware, context) = Build(_ => Task.CompletedTask);

            await middleware.InvokeAsync(context);

            context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        [Fact]
        public async Task InvokeAsync_ConKeyNotFoundException_Retorna404()
        {
            var (middleware, context) = Build(_ => throw new KeyNotFoundException("no encontrado"));

            await middleware.InvokeAsync(context);

            context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
            var response = await ReadResponse(context);
            response.Message.Should().Be("Recurso no encontrado");
            response.Error.Should().Be("no encontrado");
        }

        [Fact]
        public async Task InvokeAsync_ConUnauthorizedAccessException_Retorna401()
        {
            var (middleware, context) = Build(_ => throw new UnauthorizedAccessException("sin permiso"));

            await middleware.InvokeAsync(context);

            context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        }

        [Fact]
        public async Task InvokeAsync_ConArgumentException_Retorna400()
        {
            var (middleware, context) = Build(_ => throw new ArgumentException("argumento inválido"));

            await middleware.InvokeAsync(context);

            context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task InvokeAsync_ConExcepcionGenerica_Retorna500()
        {
            var (middleware, context) = Build(_ => throw new InvalidOperationException("fallo inesperado"));

            await middleware.InvokeAsync(context);

            context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            var response = await ReadResponse(context);
            response.Message.Should().Be("Error interno del servidor");
        }

        [Fact]
        public async Task InvokeAsync_ConExcepcion_EstableceContentTypeJson()
        {
            var (middleware, context) = Build(_ => throw new InvalidOperationException("fallo"));

            await middleware.InvokeAsync(context);

            context.Response.ContentType.Should().Be("application/json; charset=utf-8");
        }

        [Fact]
        public async Task InvokeAsync_ConExcepcion_IncluyeElPathDeLaSolicitud()
        {
            var (middleware, context) = Build(_ => throw new InvalidOperationException("fallo"));

            await middleware.InvokeAsync(context);

            var response = await ReadResponse(context);
            response.Path.Should().Be("/api/v1/test");
        }
    }
}
