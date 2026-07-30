using Connection360.Api.Models;

namespace Connection360.Api.Middleware
{
    public class NotFoundMiddleware
    {
        private readonly RequestDelegate _next;

        public NotFoundMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            if (context.Response.StatusCode == StatusCodes.Status404NotFound && !context.Response.HasStarted)
            {
                var response = new ApiResponse<object>
                {
                    Status = StatusCodes.Status404NotFound,
                    Error = "El recurso solicitado no existe",
                    Message = "Ruta no encontrada",
                    Path = context.Request.Path
                };

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(response);
            }
        }
    }
}
