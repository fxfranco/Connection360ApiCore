using Connection360Notification.Api.Filters;
using Connection360Notification.Api.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Connection360Notification.Api.Tests.Filters
{
    public class ApiResponseFilterCasosBordeTests
    {
        private readonly ApiResponseFilter _sut = new();

        private static ResultExecutingContext BuildContext(IActionResult result, String? path = "/api/v1/test")
        {
            var httpContext = new DefaultHttpContext();
            if (path is not null)
            {
                httpContext.Request.Path = path;
            }

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            return new ResultExecutingContext(actionContext, new List<IFilterMetadata>(), result, controller: new Object());
        }

        private static ResultExecutionDelegate Next(ResultExecutingContext context)
            => () => Task.FromResult(new ResultExecutedContext(context, new List<IFilterMetadata>(), context.Result, context.Controller));

        private async Task<ApiResponse<Object?>> Execute(IActionResult result, String? path = "/api/v1/test")
        {
            var context = BuildContext(result, path);
            await _sut.OnResultExecutionAsync(context, Next(context));
            var wrapped = context.Result.Should().BeOfType<ObjectResult>().Subject;
            wrapped.StatusCode.Should().NotBeNull();
            return wrapped.Value.Should().BeOfType<ApiResponse<Object?>>().Subject;
        }

        [Theory]
        [InlineData(200, "Solicitud exitosa")]
        [InlineData(201, "Recurso creado exitosamente")]
        [InlineData(204, "Sin contenido")]
        [InlineData(400, "Solicitud inválida")]
        [InlineData(401, "No autorizado")]
        [InlineData(403, "Acceso prohibido")]
        [InlineData(404, "Recurso no encontrado")]
        [InlineData(202, "Solicitud procesada")]
        [InlineData(409, "Solicitud procesada")]
        [InlineData(500, "Solicitud procesada")]
        public async Task OnResultExecutionAsync_ConObjectResult_ResuelveElMensajeSegunElStatus(Int32 status, String expectedMessage)
        {
            var response = await Execute(new ObjectResult("valor") { StatusCode = status });

            response.Status.Should().Be(status);
            response.Message.Should().Be(expectedMessage);
        }

        [Theory]
        [InlineData(200, "Solicitud exitosa", false)]
        [InlineData(204, "Sin contenido", false)]
        [InlineData(400, "Solicitud inválida", true)]
        [InlineData(401, "No autorizado", true)]
        [InlineData(403, "Acceso prohibido", true)]
        [InlineData(404, "Recurso no encontrado", true)]
        [InlineData(500, "Solicitud procesada", true)]
        public async Task OnResultExecutionAsync_ConStatusCodeResult_ConstruyeMensajeYErrorSegunElStatus(Int32 status, String expectedMessage, Boolean hasError)
        {
            var response = await Execute(new StatusCodeResult(status));

            response.Status.Should().Be(status);
            response.Message.Should().Be(expectedMessage);
            if (hasError)
            {
                response.Error.Should().Be(expectedMessage);
            }
            else
            {
                response.Error.Should().BeNull();
            }
            response.DataResponse.Should().BeNull();
            response.Meta.Should().BeNull();
        }

        [Fact]
        public async Task OnResultExecutionAsync_ConObjectResultSinStatusCode_AsumeStatus200()
        {
            var response = await Execute(new ObjectResult("dato"));

            response.Status.Should().Be(StatusCodes.Status200OK);
            response.Message.Should().Be("Solicitud exitosa");
            response.DataResponse.Should().Be("dato");
        }

        [Fact]
        public async Task OnResultExecutionAsync_ConValorNulo_DejaDataEnNull()
        {
            var response = await Execute(new ObjectResult(null) { StatusCode = 200 });

            response.DataResponse.Should().BeNull();
            response.Error.Should().BeNull();
            response.Status.Should().Be(200);
        }

        [Fact]
        public async Task OnResultExecutionAsync_ConValorNuloYStatusDeError_DejaErrorYDataEnNull()
        {
            var response = await Execute(new ObjectResult(null) { StatusCode = 404 });

            response.Error.Should().BeNull();
            response.DataResponse.Should().BeNull();
            response.Message.Should().Be("Recurso no encontrado");
        }

        [Fact]
        public async Task OnResultExecutionAsync_ConStatusDeErrorYObjetoComplejo_UsaToStringComoError()
        {
            var response = await Execute(new ObjectResult(new { Detalle = "x" }) { StatusCode = 400 });

            response.Error.Should().Contain("Detalle");
            response.DataResponse.Should().BeNull();
        }

        [Fact]
        public async Task OnResultExecutionAsync_ConStatusMenorA400_ConservaElObjetoComoData()
        {
            var data = new List<Int32> { 1, 2, 3 };

            var response = await Execute(new ObjectResult(data) { StatusCode = 201 });

            response.DataResponse.Should().BeSameAs(data);
            response.Error.Should().BeNull();
            response.Meta.Should().BeNull();
        }

        [Fact]
        public async Task OnResultExecutionAsync_SinPathEnLaPeticion_UsaCadenaVacia()
        {
            var response = await Execute(new ObjectResult("x") { StatusCode = 200 }, path: null);

            response.Path.Should().BeEmpty();
        }

        [Fact]
        public async Task OnResultExecutionAsync_StatusCodeResultSinPath_UsaCadenaVacia()
        {
            var response = await Execute(new StatusCodeResult(204), path: null);

            response.Path.Should().BeEmpty();
        }

        [Fact]
        public async Task OnResultExecutionAsync_ConPagedResultVacio_GeneraMetaConCerosYDataVacia()
        {
            var paged = new PagedResult<String> { Items = new List<String>(), TotalItems = 0, CurrentPage = 1, Limit = 0 };

            var response = await Execute(new ObjectResult(paged) { StatusCode = 200 });

            response.Meta.Should().NotBeNull();
            response.Meta!.TotalItems.Should().Be(0);
            response.Meta.TotalPages.Should().Be(0);
            response.Meta.CurrentPage.Should().Be(1);
            response.Meta.Limit.Should().Be(0);
            response.DataResponse.Should().BeAssignableTo<IEnumerable<String>>().Which.Should().BeEmpty();
        }

        [Fact]
        public async Task OnResultExecutionAsync_ConPagedResultYStatusDeError_ConservaLaRamaDePaginacion()
        {
            var paged = new PagedResult<Int32> { Items = new List<Int32> { 1 }, TotalItems = 1, CurrentPage = 1, Limit = 1 };

            var response = await Execute(new ObjectResult(paged) { StatusCode = 400 });

            response.Meta.Should().NotBeNull();
            response.Error.Should().BeNull();
            response.DataResponse.Should().BeEquivalentTo(new List<Int32> { 1 });
        }

        [Fact]
        public async Task OnResultExecutionAsync_ConResultadoYaEnvueltoYStatusDeError_NoLoModifica()
        {
            var alreadyWrapped = new ApiResponse<Object> { Status = 500, Error = "x" };
            var original = new ObjectResult(alreadyWrapped) { StatusCode = 500 };
            var context = BuildContext(original);

            await _sut.OnResultExecutionAsync(context, Next(context));

            context.Result.Should().BeSameAs(original);
            ((ObjectResult)context.Result).Value.Should().BeSameAs(alreadyWrapped);
        }

        [Fact]
        public async Task OnResultExecutionAsync_ConPagedResultDeObjetosComplejos_ExponeLosItemsTalCual()
        {
            var item = new { Id = 1 };
            var paged = new PagedResult<Object> { Items = new List<Object> { item }, TotalItems = 21, CurrentPage = 3, Limit = 10 };

            var response = await Execute(new ObjectResult(paged) { StatusCode = 200 });

            response.Meta!.TotalPages.Should().Be(3);
            response.Meta.CurrentPage.Should().Be(3);
            ((IEnumerable<Object>)response.DataResponse!).Should().ContainSingle().Which.Should().BeSameAs(item);
        }

        [Fact]
        public async Task OnResultExecutionAsync_ConObjectResult_ConservaElStatusCodeEnElResultadoFinal()
        {
            var context = BuildContext(new ObjectResult("x") { StatusCode = 201 });

            await _sut.OnResultExecutionAsync(context, Next(context));

            ((ObjectResult)context.Result).StatusCode.Should().Be(201);
        }

        [Fact]
        public async Task OnResultExecutionAsync_ConStatusCodeResult_ConservaElStatusCodeEnElResultadoFinal()
        {
            var context = BuildContext(new StatusCodeResult(403));

            await _sut.OnResultExecutionAsync(context, Next(context));

            ((ObjectResult)context.Result).StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task OnResultExecutionAsync_ConResultadoVacio_NoLoModificaYLlamaANext()
        {
            var empty = new EmptyResult();
            var context = BuildContext(empty);
            Boolean called = false;

            await _sut.OnResultExecutionAsync(context, () =>
            {
                called = true;
                return Task.FromResult(new ResultExecutedContext(context, new List<IFilterMetadata>(), context.Result, context.Controller));
            });

            context.Result.Should().BeSameAs(empty);
            called.Should().BeTrue();
        }

        [Fact]
        public async Task OnResultExecutionAsync_GeneraTimestampUtcReciente()
        {
            DateTime before = DateTime.UtcNow.AddSeconds(-5);

            var response = await Execute(new ObjectResult("x") { StatusCode = 200 });

            response.Timestamp.Should().BeAfter(before);
            response.Timestamp.Should().BeOnOrBefore(DateTime.UtcNow.AddSeconds(5));
        }
    }
}
