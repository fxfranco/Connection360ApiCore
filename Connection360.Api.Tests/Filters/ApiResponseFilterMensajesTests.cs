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
    public class ApiResponseFilterMensajesTests
    {
        private readonly ApiResponseFilter _sut = new();

        private static ResultExecutingContext BuildContext(IActionResult result, String? path = "/api/v1/test")
        {
            var httpContext = new DefaultHttpContext();
            if (path != null) httpContext.Request.Path = path;
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            return new ResultExecutingContext(actionContext, new List<IFilterMetadata>(), result, controller: new Object());
        }

        private static ResultExecutionDelegate Next(ResultExecutingContext context)
            => () => Task.FromResult(new ResultExecutedContext(context, new List<IFilterMetadata>(), context.Result, context.Controller));

        [Theory]
        [InlineData(200, "Solicitud exitosa")]
        [InlineData(201, "Recurso creado exitosamente")]
        [InlineData(204, "Sin contenido")]
        [InlineData(400, "Solicitud inválida")]
        [InlineData(401, "No autorizado")]
        [InlineData(403, "Acceso prohibido")]
        [InlineData(404, "Recurso no encontrado")]
        [InlineData(409, "Solicitud procesada")]
        [InlineData(500, "Solicitud procesada")]
        public async Task OnResultExecutionAsync_ConObjectResult_ResuelveElMensajePorStatus(Int32 status, String expected)
        {
            var context = BuildContext(new ObjectResult("valor") { StatusCode = status });

            await _sut.OnResultExecutionAsync(context, Next(context));

            var wrapped = context.Result.Should().BeOfType<ObjectResult>().Subject;
            wrapped.StatusCode.Should().Be(status);
            wrapped.Value.Should().BeOfType<ApiResponse<Object?>>().Which.Message.Should().Be(expected);
        }

        [Theory]
        [InlineData(204, "Sin contenido", false)]
        [InlineData(400, "Solicitud inválida", true)]
        [InlineData(401, "No autorizado", true)]
        [InlineData(403, "Acceso prohibido", true)]
        [InlineData(404, "Recurso no encontrado", true)]
        [InlineData(418, "Solicitud procesada", true)]
        public async Task OnResultExecutionAsync_ConStatusCodeResult_ResuelveMensajeYError(Int32 status, String expectedMessage, Boolean hasError)
        {
            var context = BuildContext(new StatusCodeResult(status));

            await _sut.OnResultExecutionAsync(context, Next(context));

            var response = context.Result.Should().BeOfType<ObjectResult>().Which.Value.Should().BeOfType<ApiResponse<Object?>>().Subject;
            response.Status.Should().Be(status);
            response.Message.Should().Be(expectedMessage);
            (response.Error != null).Should().Be(hasError);
            response.DataResponse.Should().BeNull();
        }

        [Fact]
        public async Task OnResultExecutionAsync_ConObjectResultSinStatusCode_AsumeStatus200()
        {
            var context = BuildContext(new ObjectResult("valor"));

            await _sut.OnResultExecutionAsync(context, Next(context));

            var wrapped = context.Result.Should().BeOfType<ObjectResult>().Subject;
            wrapped.StatusCode.Should().Be(200);
            wrapped.Value.Should().BeOfType<ApiResponse<Object?>>().Which.DataResponse.Should().Be("valor");
        }

        [Fact]
        public async Task OnResultExecutionAsync_ConObjectResultConValorNulo_EnvuelveConDataNula()
        {
            var context = BuildContext(new ObjectResult(null) { StatusCode = 200 });

            await _sut.OnResultExecutionAsync(context, Next(context));

            var response = context.Result.Should().BeOfType<ObjectResult>().Which.Value.Should().BeOfType<ApiResponse<Object?>>().Subject;
            response.DataResponse.Should().BeNull();
            response.Error.Should().BeNull();
        }

        [Fact]
        public async Task OnResultExecutionAsync_ConErrorYValorNulo_ErrorQuedaNulo()
        {
            var context = BuildContext(new ObjectResult(null) { StatusCode = 500 });

            await _sut.OnResultExecutionAsync(context, Next(context));

            var response = context.Result.Should().BeOfType<ObjectResult>().Which.Value.Should().BeOfType<ApiResponse<Object?>>().Subject;
            response.Error.Should().BeNull();
            response.DataResponse.Should().BeNull();
        }

        [Fact]
        public async Task OnResultExecutionAsync_ConPagedResultYStatusDeError_PriorizaLaPaginacion()
        {
            var paged = new PagedResult<Object> { Items = ["a", "b"], TotalItems = 2, CurrentPage = 1, Limit = 1 };
            var context = BuildContext(new ObjectResult(paged) { StatusCode = 200 });

            await _sut.OnResultExecutionAsync(context, Next(context));

            var response = context.Result.Should().BeOfType<ObjectResult>().Which.Value.Should().BeOfType<ApiResponse<Object?>>().Subject;
            response.Meta.Should().NotBeNull();
            response.Meta!.TotalPages.Should().Be(2);
            response.Meta.CurrentPage.Should().Be(1);
            response.Meta.Limit.Should().Be(1);
            ((IEnumerable<Object>)response.DataResponse!).Should().HaveCount(2);
        }

        [Fact]
        public async Task OnResultExecutionAsync_SinPathEnLaSolicitud_UsaCadenaVacia()
        {
            var context = BuildContext(new ObjectResult("x") { StatusCode = 200 }, path: null);

            await _sut.OnResultExecutionAsync(context, Next(context));

            context.Result.Should().BeOfType<ObjectResult>().Which.Value.Should().BeOfType<ApiResponse<Object?>>().Which.Path.Should().Be(String.Empty);
        }

        [Fact]
        public async Task OnResultExecutionAsync_ConStatusCodeResultSinPath_UsaCadenaVacia()
        {
            var context = BuildContext(new StatusCodeResult(200), path: null);

            await _sut.OnResultExecutionAsync(context, Next(context));

            context.Result.Should().BeOfType<ObjectResult>().Which.Value.Should().BeOfType<ApiResponse<Object?>>().Which.Path.Should().Be(String.Empty);
        }
    }
}
