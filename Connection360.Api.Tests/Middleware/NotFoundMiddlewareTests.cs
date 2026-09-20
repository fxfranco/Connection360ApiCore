using Connection360.Api.Middleware;
using Connection360.Api.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Xunit;

namespace Connection360.Api.Tests.Middleware
{
    public class NotFoundMiddlewareTests
    {
        private static DefaultHttpContext BuildContext(String path = "/api/v1/no-existe")
        {
            var context = new DefaultHttpContext();
            context.Request.Path = path;
            context.Response.Body = new MemoryStream();
            return context;
        }

        [Fact]
        public async Task InvokeAsync_CuandoElStatusEs404_EscribeElCuerpoDeRespuestaEstandar()
        {
            var context = BuildContext();
            var middleware = new NotFoundMiddleware(_ =>
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            var json = await reader.ReadToEndAsync();
            var response = JsonSerializer.Deserialize<ApiResponse<Object>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

            response.Status.Should().Be(404);
            response.Message.Should().Be("Ruta no encontrada");
            response.Error.Should().Be("El recurso solicitado no existe");
        }

        [Fact]
        public async Task InvokeAsync_CuandoElStatusNoEs404_NoModificaElCuerpo()
        {
            var context = BuildContext();
            var middleware = new NotFoundMiddleware(_ =>
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(context);

            context.Response.Body.Length.Should().Be(0);
        }

        [Fact]
        public async Task InvokeAsync_SiLaRespuestaYaComenzo_NoIntentaEscribirDeNuevo()
        {
            var context = BuildContext();
            var middleware = new NotFoundMiddleware(async ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                await ctx.Response.WriteAsync("ya-escrito");
            });

            Func<Task> act = () => middleware.InvokeAsync(context);

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task InvokeAsync_SiempreLlamaAlSiguienteMiddleware()
        {
            var context = BuildContext();
            Boolean called = false;
            var middleware = new NotFoundMiddleware(_ =>
            {
                called = true;
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(context);

            called.Should().BeTrue();
        }
    }
}
