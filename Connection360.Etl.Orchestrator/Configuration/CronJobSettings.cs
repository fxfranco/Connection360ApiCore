namespace Connection360.Etl.Orchestrator.Configuration
{
    /// <summary>
    /// Configuración de un trabajo programado del orquestador: qué expresión cron dispara su
    /// ejecución y qué proceso externo lanzar cuando corresponde. Pensado para que
    /// Connection360.Etl.App sea el primer (y hoy único) consumidor, pero sin acoplarse a él: el
    /// orquestador solo sabe lanzar procesos por su ruta y argumentos, nunca referencia tipos de
    /// Connection360.Etl.App. Agregar un nuevo trabajo programado (por ejemplo, otro ejecutable) es
    /// solo una entrada más en "Orchestrator:CronJobs" del appsettings, sin cambios de código.
    /// </summary>
    public sealed class CronJobSettings
    {
        /// <summary>Nombre identificador del trabajo, usado únicamente para los logs (prefijo "[Nombre] ...").</summary>
        public String Name { get; set; } = default!;

        /// <summary>Si es false, el trabajo queda configurado pero nunca se programa ni ejecuta.</summary>
        public Boolean Enabled { get; set; } = true;

        /// <summary>Expresión cron estándar de 5 campos: minuto hora día-mes mes día-semana.</summary>
        public String CronExpression { get; set; } = default!;

        /// <summary>
        /// Id de zona horaria (IANA, ej. "America/Bogota") en la que se evalúa <see cref="CronExpression"/>.
        /// Si es null o vacío, se usa la zona horaria local del servidor donde corre el orquestador.
        /// </summary>
        public String? TimeZoneId { get; set; }

        /// <summary>Ejecutable a lanzar. Por defecto "dotnet", para correr un .dll publicado.</summary>
        public String ExecutablePath { get; set; } = "dotnet";

        /// <summary>
        /// Argumentos del ejecutable, típicamente el nombre del .dll publicado de
        /// Connection360.Etl.App (relativo a <see cref="WorkingDirectory"/>).
        /// </summary>
        public String Arguments { get; set; } = default!;

        /// <summary>
        /// Carpeta de trabajo del proceso lanzado: es clave que sea la carpeta donde está publicado
        /// Connection360.Etl.App, para que cargue SU PROPIO appsettings.json/appsettings.Development.json
        /// (puede ser relativa al directorio base del orquestador, o absoluta).
        /// </summary>
        public String WorkingDirectory { get; set; } = default!;

        /// <summary>
        /// Tiempo máximo (en minutos) que se espera a que termine una corrida antes de matar el
        /// proceso y marcarla como fallida (protección ante una API externa que nunca responde).
        /// Null = sin límite.
        /// </summary>
        public Int32? TimeoutMinutes { get; set; }
    }
}
