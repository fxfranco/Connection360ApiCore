using System.Diagnostics;

namespace Connection360.ApiGateway.Middleware
{
    /// <summary>
    /// Logging estructurado de cada request que pasa por el Gateway (trazabilidad / auditoria).
    /// </summary>
    public sealed class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                await _next(context);
                sw.Stop();

                _logger.LogInformation(
                    "{Method} {Path} -> {StatusCode} ({ElapsedMs}ms) [TraceId: {TraceId}]",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    sw.ElapsedMilliseconds,
                    context.TraceIdentifier);
            }
            catch (Exception ex)
            {
                sw.Stop();

                // Si la respuesta ya inició (común en YARP con WebSockets/SignalR al abortar transmisión)
                if (context.Response.HasStarted)
                {
                    _logger.LogWarning(
                        ex,
                        "{Method} {Path} -> Interrumpido/Cancelado después de iniciar respuesta ({ElapsedMs}ms) [TraceId: {TraceId}]",
                        context.Request.Method,
                        context.Request.Path,
                        sw.ElapsedMilliseconds,
                        context.TraceIdentifier);

                    // Si la respuesta ya inició, la excepción fue manejada/logueada. 
                    // No la propagamos para evitar el bucle de middleware.
                    context.Abort();
                    return;
                }

                // Si la respuesta NO ha iniciado, logueamos el error y dejamos que ExceptionHandlingMiddleware la maneje
                _logger.LogError(
                    ex,
                    "{Method} {Path} -> Falló antes de enviar respuesta ({ElapsedMs}ms) [TraceId: {TraceId}]",
                    context.Request.Method,
                    context.Request.Path,
                    sw.ElapsedMilliseconds,
                    context.TraceIdentifier);

                throw;
            }
        }
    }
}
