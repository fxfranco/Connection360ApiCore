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
    }
}
