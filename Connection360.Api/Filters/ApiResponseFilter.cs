using Connection360.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
//using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Connection360.Api.Filters
{
    public class ApiResponseFilter : IAsyncResultFilter
    {

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (context.Result is ObjectResult objectResult)
            {
                var path = context.HttpContext.Request.Path.Value ?? String.Empty;
                var status = objectResult.StatusCode ?? StatusCodes.Status200OK;

                Boolean alreadyWrapped = objectResult.Value is not null &&
                    objectResult.Value.GetType().IsGenericType &&
                    objectResult.Value.GetType().GetGenericTypeDefinition() == typeof(ApiResponse<>);

                if (!alreadyWrapped)
                {
                    MetaResponse? meta = null;
                    Object? data = objectResult.Value;
                    String? error = null;

                    var valueType = objectResult.Value?.GetType();
                    if (valueType is { IsGenericType: true } &&
                        valueType.GetGenericTypeDefinition() == typeof(PagedResult<>))
                    {
                        dynamic paged = objectResult.Value!;
                        meta = new MetaResponse
                        {
                            TotalItems = paged.TotalItems,
                            TotalPages = paged.TotalPages,
                            CurrentPage = paged.CurrentPage,
                            Limit = paged.Limit
                        };
                        data = paged.Items;
                    }
                    else if (status >= 400)
                    {
                        error = objectResult.Value?.ToString();
                        data = null;
                    }

                    var wrapped = new ApiResponse<object?>
                    {
                        Status = status,
                        Error = error,
                        Message = ResolveMessage(status),
                        DataResponse = data,
                        Meta = meta,
                        Path = path
                    };

                    context.Result = new ObjectResult(wrapped) { StatusCode = status };
                }
            }
            else if (context.Result is StatusCodeResult statusResult)
            {
                var status = statusResult.StatusCode;
                var path = context.HttpContext.Request.Path.Value ?? String.Empty;

                var wrapped = new ApiResponse<object?>
                {
                    Status = status,
                    Error = status >= 400 ? ResolveMessage(status) : null,
                    Message = ResolveMessage(status),
                    Path = path
                };

                context.Result = new ObjectResult(wrapped) { StatusCode = status };
            }

            await next();
        }

        private static String ResolveMessage(Int32 status) => status switch
        {
            StatusCodes.Status200OK => "Solicitud exitosa",
            StatusCodes.Status201Created => "Recurso creado exitosamente",
            StatusCodes.Status204NoContent => "Sin contenido",
            StatusCodes.Status400BadRequest => "Solicitud inválida",
            StatusCodes.Status401Unauthorized => "No autorizado",
            StatusCodes.Status403Forbidden => "Acceso prohibido",
            StatusCodes.Status404NotFound => "Recurso no encontrado",
            _ => "Solicitud procesada"
        };
    }
}
