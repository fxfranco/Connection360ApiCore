namespace Connection360.ApiGateway.Middleware
{
    /// <summary>
    /// Agrega cabeceras de seguridad recomendadas por OWASP Secure Headers Project.
    /// Mitiga: Clickjacking, MIME sniffing, XSS, filtrado de referrer, permisos de features del navegador.
    /// </summary>
    public sealed class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            var headers = context.Response.Headers;

            // OWASP A05:2021 - Security Misconfiguration
            headers.Append("X-Content-Type-Options", "nosniff");
            headers.Append("X-Frame-Options", "DENY");
            headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
            headers.Append("X-XSS-Protection", "0"); // deprecado en navegadores modernos; se confia en CSP
            headers.Append("Content-Security-Policy",
                "default-src 'self'; frame-ancestors 'none'; base-uri 'self'; object-src 'none'");

            // Elimina cabeceras que revelan tecnologia usada (Server, X-Powered-By)
            headers.Remove("Server");
            headers.Remove("X-Powered-By");

            await _next(context);
        }
    }
}
