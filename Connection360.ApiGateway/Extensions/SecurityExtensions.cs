namespace Connection360.ApiGateway.Extensions
{
    /// <summary>
    /// Configuraciones adicionales de "hardening" del servidor Kestrel.
    /// </summary>
    public static class SecurityExtensions
    {
        public static WebApplicationBuilder ConfigureSecureKestrel(this WebApplicationBuilder builder)
        {
            builder.WebHost.ConfigureKestrel(options =>
            {
                // Limites anti-DoS: tamaño maximo de body, headers y tiempo de request
                options.Limits.MaxRequestBodySize = 5 * 1024 * 1024; // 5 MB
                options.Limits.MaxRequestHeaderCount = 50;
                options.Limits.MaxRequestHeadersTotalSize = 32 * 1024;
                options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(10);
                options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);

                // TLS 1.2 minimo (OWASP: nunca permitir SSL3/TLS1.0/1.1)
                options.ConfigureHttpsDefaults(https =>
                {
                    https.SslProtocols = System.Security.Authentication.SslProtocols.Tls12
                                        | System.Security.Authentication.SslProtocols.Tls13;
                });
            });

            return builder;
        }
    }
}
