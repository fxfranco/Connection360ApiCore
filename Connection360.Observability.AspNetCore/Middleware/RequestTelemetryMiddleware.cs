using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Connection360.Observability.AspNetCore.Middleware
{
    /// <summary>
    /// ASP.NET Core ya crea un span por cada petición ("Microsoft.AspNetCore.Hosting.HttpRequestIn"), pero
    /// sin ningún dato de la petición (eso lo agregan librerías externas como OpenTelemetry). Este middleware,
    /// escrito solo con <see cref="Activity"/> nativo, completa ese span con las convenciones semánticas de
    /// OpenTelemetry: método, ruta, esquema, host, estado de la respuesta y la plantilla de la ruta
    /// (<c>http.route</c>, p. ej. "/api/v1/home/totals", que agrupa las peticiones sin cardinalidad infinita).
    /// <para>
    /// No registra la cadena de consulta (<c>?clave=valor</c>): puede traer datos personales o secretos.
    /// Si nadie escucha trazas (o la petición no fue muestreada) no hace nada más que llamar al siguiente middleware.
    /// </para>
    /// </summary>
    public sealed class RequestTelemetryMiddleware
    {
        private const Int32 MaxUserAgentLength = 256;

        private readonly RequestDelegate _next;

        public RequestTelemetryMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            Activity? activity = Activity.Current;
            if (activity is null || !activity.IsAllDataRequested)
            {
                await _next(context).ConfigureAwait(false);
                return;
            }

            HttpRequest request = context.Request;
            activity.SetTag("http.request.method", request.Method);
            activity.SetTag("url.path", request.Path.Value);
            activity.SetTag("url.scheme", request.Scheme);
            if (request.Host.HasValue)
            {
                activity.SetTag("server.address", request.Host.Host);
                if (request.Host.Port is { } port)
                {
                    activity.SetTag("server.port", port);
                }
            }

            String? userAgent = request.Headers.UserAgent.ToString();
            if (!String.IsNullOrEmpty(userAgent))
            {
                activity.SetTag("user_agent.original", userAgent.Length > MaxUserAgentLength ? userAgent[..MaxUserAgentLength] : userAgent);
            }

            try
            {
                await _next(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // La excepción sigue su camino (la atiende el manejador de errores de la aplicación); aquí solo se anota.
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity.SetTag("error.type", ex.GetType().FullName);
                activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
                {
                    { "exception.type", ex.GetType().FullName },
                    { "exception.message", ex.Message },
                }));
                throw;
            }
            finally
            {
                Complete(context, activity);
            }
        }

        private static void Complete(HttpContext context, Activity activity)
        {
            Int32 status = context.Response.StatusCode;
            activity.SetTag("http.response.status_code", status);

            String? route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText;
            if (!String.IsNullOrEmpty(route))
            {
                // Los controladores declaran la ruta sin "/" inicial ("api/v1/..."); la convención lleva "/".
                route = route.StartsWith('/') ? route : "/" + route;
                activity.SetTag("http.route", route);
            }

            // Convención de OpenTelemetry para spans de servidor: "{método} {ruta}".
            activity.DisplayName = String.IsNullOrEmpty(route) ? context.Request.Method : $"{context.Request.Method} {route}";

            if (status >= 500)
            {
                // Si el catch ya anotó la excepción (estado y descripción), no se sobrescribe.
                if (activity.Status != ActivityStatusCode.Error)
                {
                    activity.SetTag("error.type", status.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    activity.SetStatus(ActivityStatusCode.Error);
                }
            }
        }
    }

    public static class RequestTelemetryApplicationBuilderExtensions
    {
        /// <summary>
        /// Enriquece el span de cada petición con método, ruta, estado, etc. Debe registrarse lo más arriba posible
        /// del pipeline (antes del manejo de excepciones), para ver el estado final de la respuesta.
        /// </summary>
        public static IApplicationBuilder UseConnection360RequestTelemetry(this IApplicationBuilder app)
            => app.UseMiddleware<RequestTelemetryMiddleware>();
    }
}
