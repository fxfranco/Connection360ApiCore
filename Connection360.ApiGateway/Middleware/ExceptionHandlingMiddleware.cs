using System.Net;
using System.Text.Json;

namespace Connection360.ApiGateway.Middleware
{
    /// <summary>
    /// Captura toda excepcion no controlada y responde con un formato uniforme,
    /// SIN filtrar stack traces ni detalles internos (OWASP A09:2021 - Logging & Error Handling).
    /// </summary>
    public sealed class ExceptionHandlingMiddleware
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
                _logger.LogError(ex, "Error no controlado procesando {Path}", context.Request.Path);

                // Si la respuesta ya comenzó a escribirse (ej: WebSockets / SignalR Streaming)
                // NO se pueden reescribir los encabezados ni el cuerpo JSON.
                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("La respuesta ya había comenzado a enviarse para {Path}. No se pudo modificar la respuesta de error.", context.Request.Path);
                    // Forzar el cierre limpio del socket en el pipeline sin propagar la excepción
                    context.Abort();
                    return;
                }

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                String payload = JsonSerializer.Serialize(new
                {
                    title = "Ha ocurrido un error inesperado.",
                    status = 500,
                    traceId = context.TraceIdentifier
                });

                await context.Response.WriteAsync(payload);
            }
        }
    }
}
