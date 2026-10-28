using Connection360.Api.Models;
using Connection360.Domain.Exceptions;

namespace Connection360.Api.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error no controlado en {Path}", context.Request.Path);

                var (status, message) = ex switch
                {
                    DuplicateRecordException => (StatusCodes.Status409Conflict, "El registro ya existe"),
                    KeyNotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado"),
                    UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "No autorizado"),
                    ArgumentException => (StatusCodes.Status400BadRequest, "Solicitud inválida"),
                    _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor")
                };

                var response = new ApiResponse<object>
                {
                    Status = status,
                    Error = ex.Message,
                    Message = message,
                    Path = context.Request.Path
                };

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = status;
                await context.Response.WriteAsJsonAsync(response, context.RequestAborted);
            }
        }
    }
}
