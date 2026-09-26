namespace Connection360Notification.Api.Models
{
    /// <summary>
    /// Envoltorio estándar de todas las respuestas de la API. Todas las respuestas que devuelven
    /// un <c>ObjectResult</c> o un <c>StatusCodeResult</c> desde un controlador son envueltas en
    /// tiempo de ejecución en esta forma por <see cref="Connection360Notification.Api.Filters.ApiResponseFilter"/>,
    /// después de que la acción del controlador se ejecuta. Esto significa que el esquema de
    /// respuesta que Swagger muestra para cada endpoint (basado en la firma del controlador) no
    /// coincide con la forma real de la respuesta JSON, que siempre es <c>ApiResponse&lt;T&gt;</c>.
    /// </summary>
    /// <typeparam name="T">Tipo de los datos contenidos en <see cref="DataResponse"/>.</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>Fecha y hora (UTC) en que se generó la respuesta.</summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>Código de estado HTTP de la respuesta.</summary>
        public Int32 Status { get; set; }

        /// <summary>Descripción del error, presente únicamente cuando <see cref="Status"/> representa un error (código &gt;= 400).</summary>
        public String? Error { get; set; }

        /// <summary>Mensaje descriptivo asociado al resultado de la operación.</summary>
        public String? Message { get; set; }

        /// <summary>Datos de la respuesta. Cuando el valor original era un <see cref="PagedResult{T}"/>, contiene únicamente los elementos de la página (los metadatos de paginación se exponen por separado en <see cref="Meta"/>).</summary>
        public T? DataResponse { get; set; }

        /// <summary>Metadatos de paginación, presentes únicamente cuando el resultado original era un <see cref="PagedResult{T}"/>.</summary>
        public MetaResponse? Meta { get; set; }

        /// <summary>Ruta del endpoint que generó la respuesta.</summary>
        public String? Path { get; set; }
    }
}
