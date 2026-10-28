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
    public class ApiResponseFilterProblemDetailsTests
    {
        private static async Task<ApiResponse<Object?>> Execute(IActionResult result)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/api/v1/test";
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var context = new ResultExecutingContext(actionContext, new List<IFilterMetadata>(), result, controller: new Object());

            await new ApiResponseFilter().OnResultExecutionAsync(
                context,
                () => Task.FromResult(new ResultExecutedContext(context, new List<IFilterMetadata>(), context.Result, context.Controller)));

            var wrapped = context.Result.Should().BeOfType<ObjectResult>().Subject;
            return wrapped.Value.Should().BeOfType<ApiResponse<Object?>>().Subject;
        }

        [Fact]
        public async Task OnResultExecutionAsync_ConProblemDetailsConDetalle_UsaElDetalleComoError()
        {
            var problem = new ProblemDetails { Status = 500, Title = "Error", Detail = "Detalle del problema" };

            var response = await Execute(new ObjectResult(problem) { StatusCode = 500 });

            response.Error.Should().Be("Detalle del problema");
            response.DataResponse.Should().BeNull();
        }

        [Fact]
        public async Task OnResultExecutionAsync_ConProblemDetailsSinDetalle_UsaElTituloComoError()
        {
            var problem = new ProblemDetails { Status = 404, Title = "Not Found" };

            var response = await Execute(new ObjectResult(problem) { StatusCode = 404 });

            response.Error.Should().Be("Not Found");
            response.Message.Should().Be("Recurso no encontrado");
        }

        [Fact]
        public async Task OnResultExecutionAsync_ConProblemDetailsDeNotFoundResult_NoExponeElNombreDelTipo()
        {
            // Es lo que [ApiController] produce al convertir NotFound() en ProblemDetails.
            var response = await Execute(new ObjectResult(new ProblemDetails { Status = 404, Title = "Not Found" }) { StatusCode = 404 });

            response.Error.Should().NotContain("ProblemDetails");
        }

        [Fact]
        public async Task OnResultExecutionAsync_ConErrorDeTextoPlano_ConservaElTexto()
        {
            var response = await Execute(new ObjectResult("Acceso denegado") { StatusCode = 403 });

            response.Error.Should().Be("Acceso denegado");
        }
    }
}
