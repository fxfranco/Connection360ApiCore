namespace Connection360.Api.Models
{
    /// <summary>
    /// Envoltorio (envelope) estándar de todas las respuestas HTTP de la API. Lo aplica
    /// automáticamente <see cref="Connection360.Api.Filters.ApiResponseFilter"/> sobre el resultado
    /// de cada acción, así que los controladores no lo construyen directamente (salvo para
    /// respuestas de error de validación) y su forma real en cada respuesta es
    /// <c>ApiResponse&lt;T&gt;</c>, donde <typeparamref name="T"/> es el tipo de dato devuelto por
    /// el endpoint (por ejemplo <c>ApiResponse&lt;ClientSummaryResponse&gt;</c>).
    /// </summary>
    /// <typeparam name="T">Tipo del dato de negocio devuelto en <see cref="DataResponse"/>.</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>Fecha y hora (UTC) en la que se generó la respuesta.</summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>Código de estado HTTP de la respuesta (200, 201, 400, 404, etc.).</summary>
        public Int32 Status { get; set; }

        /// <summary>Mensaje de error, solo presente cuando <see cref="Status"/> indica una falla (&gt;= 400).</summary>
        public String? Error { get; set; }

        /// <summary>Mensaje descriptivo y amigable del resultado de la operación.</summary>
        public String? Message { get; set; }

        /// <summary>Dato de negocio devuelto por el endpoint. Es <c>null</c> en respuestas de error o sin contenido.</summary>
        public T? DataResponse { get; set; }

        /// <summary>Metadatos de paginación, presentes únicamente cuando el endpoint devuelve un listado paginado.</summary>
        public MetaResponse? Meta { get; set; }

        /// <summary>Ruta del endpoint que generó la respuesta (para trazabilidad/diagnóstico).</summary>
        public String? Path { get; set; }
    }
}
