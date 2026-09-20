using Connection360.Api.Filters;
using Connection360.Api.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Connection360.Api.Tests.Filters
{
    public class ApiResponseFilterTests
    {
        private readonly ApiResponseFilter _sut = new();

        private static ResultExecutingContext BuildContext(IActionResult result, String path = "/api/v1/test")
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = path;

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            return new ResultExecutingContext(actionContext, new List<IFilterMetadata>(), result, controller: new Object());
        }

        private static ResultExecutionDelegate NextDelegate(ResultExecutingContext context)
        {
            return () => Task.FromResult(new ResultExecutedContext(context, new List<IFilterMetadata>(), context.Result, context.Controller));
        }

        [Fact]
        public async Task OnResultExecutionAsync_ConObjectResultSimple_LoEnvuelveEnApiResponse()
        {
            var objectResult = new ObjectResult("dato-simple") { StatusCode = StatusCodes.Status200OK };
            var context = BuildContext(objectResult);

            await _sut.OnResultExecutionAsync(context, NextDelegate(context));

            var wrapped = context.Result.Should().BeOfType<ObjectResult>().Subject;
            var apiResponse = wrapped.Value.Should().BeOfType<ApiResponse<Object?>>().Subject;
            apiResponse.Status.Should().Be(200);
            apiResponse.Message.Should().Be("Solicitud exitosa");
            apiResponse.DataResponse.Should().Be("dato-simple");
            apiResponse.Path.Should().Be("/api/v1/test");
        }

        [Fact]
        public async Task OnResultExecutionAsync_ConResultadoYaEnvuelto_NoLoVuelveAEnvolver()
        {
            var alreadyWrapped = new ApiResponse<String> { Status = 200, DataResponse = "x" };
            var objectResult = new ObjectResult(alreadyWrapped) { StatusCode = StatusCodes.Status200OK };
            var context = BuildContext(objectResult);

            await _sut.OnResultExecutionAsync(context, NextDelegate(context));

            var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
            result.Value.Should().BeSameAs(alreadyWrapped);
        }

        [Fact]
        public async Task OnResultExecutionAsync_ConPagedResult_ExtraeMetaYUsaItemsComoData()
        {
            var paged = new PagedResult<String> { Items = new List<String> { "a", "b" }, TotalItems = 20, CurrentPage = 1, Limit = 10 };
            var objectResult = new ObjectResult(paged) { StatusCode = StatusCodes.Status200OK };
            var context = BuildContext(objectResult);

            await _sut.OnResultExecutionAsync(context, NextDelegate(context));

            var wrapped = context.Result.Should().BeOfType<ObjectResult>().Subject;
            var apiResponse = wrapped.Value.Should().BeOfType<ApiResponse<Object?>>().Subject;
            apiResponse.Meta.Should().NotBeNull();
            apiResponse.Meta!.TotalItems.Should().Be(20);
            apiResponse.Meta.TotalPages.Should().Be(2);
            apiResponse.DataResponse.Should().BeEquivalentTo(new List<String> { "a", "b" });
        }

        [Fact]
        public async Task OnResultExecutionAsync_ConStatusDeError_MapeaElValorComoErrorYDataEnNull()
        {
            var objectResult = new ObjectResult("mensaje de error") { StatusCode = StatusCodes.Status400BadRequest };
            var context = BuildContext(objectResult);

            await _sut.OnResultExecutionAsync(context, NextDelegate(context));

            var wrapped = context.Result.Should().BeOfType<ObjectResult>().Subject;
            var apiResponse = wrapped.Value.Should().BeOfType<ApiResponse<Object?>>().Subject;
            apiResponse.Error.Should().Be("mensaje de error");
            apiResponse.DataResponse.Should().BeNull();
            apiResponse.Message.Should().Be("Solicitud inválida");
        }

        [Fact]
        public async Task OnResultExecutionAsync_ConStatusCodeResult_ConstruyeUnaApiResponseSinData()
        {
            var statusResult = new StatusCodeResult(StatusCodes.Status204NoContent);
            var context = BuildContext(statusResult);

            await _sut.OnResultExecutionAsync(context, NextDelegate(context));

            var wrapped = context.Result.Should().BeOfType<ObjectResult>().Subject;
            var apiResponse = wrapped.Value.Should().BeOfType<ApiResponse<Object?>>().Subject;
            apiResponse.Status.Should().Be(204);
            apiResponse.Message.Should().Be("Sin contenido");
            apiResponse.Error.Should().BeNull();
        }

        [Fact]
        public async Task OnResultExecutionAsync_ConStatusCodeResultDeError_IncluyeElMensajeComoError()
        {
            var statusResult = new StatusCodeResult(StatusCodes.Status404NotFound);
            var context = BuildContext(statusResult);

            await _sut.OnResultExecutionAsync(context, NextDelegate(context));

            var wrapped = context.Result.Should().BeOfType<ObjectResult>().Subject;
            var apiResponse = wrapped.Value.Should().BeOfType<ApiResponse<Object?>>().Subject;
            apiResponse.Error.Should().Be("Recurso no encontrado");
        }

        [Fact]
        public async Task OnResultExecutionAsync_ConOtroTipoDeResultado_NoLoModifica()
        {
            var redirectResult = new RedirectResult("/otra-ruta");
            var context = BuildContext(redirectResult);

            await _sut.OnResultExecutionAsync(context, NextDelegate(context));

            context.Result.Should().BeSameAs(redirectResult);
        }

        [Fact]
        public async Task OnResultExecutionAsync_SiempreInvocaElSiguienteDelegado()
        {
            var objectResult = new ObjectResult("data") { StatusCode = StatusCodes.Status200OK };
            var context = BuildContext(objectResult);
            Boolean nextCalled = false;

            ResultExecutionDelegate next = () =>
            {
                nextCalled = true;
                return Task.FromResult(new ResultExecutedContext(context, new List<IFilterMetadata>(), context.Result, context.Controller));
            };

            await _sut.OnResultExecutionAsync(context, next);

            nextCalled.Should().BeTrue();
        }
    }
}
