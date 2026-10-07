namespace Connection360.Observability.Domain.Models
{
    /// <summary>
    /// Nombres con los que se identifica cada aplicación en la telemetría (campo "service" de
    /// cada documento de logs, métricas y trazas). Son cadenas (y no un enum) para que una
    /// aplicación nueva pueda registrarse sin modificar este proyecto.
    /// </summary>
    public static class ObservedServices
    {
        /// <summary>Connection360.Api</summary>
        public const String ApiCore = "ApiCore";

        /// <summary>Connection360Notification.Api</summary>
        public const String ApiNotification = "ApiNotification";

        /// <summary>Connection360.Etl.App</summary>
        public const String Etl = "Etl";
    }

    /// <summary>
    /// "Recurso" (en términos de OpenTelemetry) que genera la telemetría: qué aplicación es, de qué
    /// versión, en qué ambiente y qué instancia concreta (equipo + proceso). Se copia a cada registro.
    /// </summary>
    /// <param name="ServiceName">Una de <see cref="ObservedServices"/> (ApiCore, ApiNotification, Etl).</param>
    /// <param name="ServiceVersion">Versión del ensamblado de la aplicación.</param>
    /// <param name="Environment">Ambiente (Development, Production, ...).</param>
    /// <param name="InstanceId">Identificador de la instancia: equipo y proceso.</param>
    public sealed record ServiceIdentity(String ServiceName, String ServiceVersion, String Environment, String InstanceId);
}
