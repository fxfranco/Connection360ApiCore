namespace Connection360Notification.Api.Models
{
    /// <summary>
    /// Resultado paginado devuelto internamente por los controladores. No es la forma final de la
    /// respuesta JSON: <see cref="Connection360Notification.Api.Filters.ApiResponseFilter"/> lo
    /// transforma en tiempo de ejecución, colocando <see cref="Items"/> en <c>ApiResponse&lt;T&gt;.DataResponse</c>
    /// y el resto de las propiedades en <c>ApiResponse&lt;T&gt;.Meta</c>.
    /// </summary>
    /// <typeparam name="T">Tipo de los elementos de la página.</typeparam>
    public class PagedResult<T>
    {
        /// <summary>Elementos de la página actual.</summary>
        public IEnumerable<T> Items { get; set; } = [];

        /// <summary>Cantidad total de elementos disponibles (sin paginar).</summary>
        public Int64 TotalItems { get; set; }

        /// <summary>Número de la página actual.</summary>
        public Int64 CurrentPage { get; set; }

        /// <summary>Cantidad máxima de elementos por página.</summary>
        public Int64 Limit { get; set; }

        /// <summary>Cantidad total de páginas, calculada a partir de <see cref="TotalItems"/> y <see cref="Limit"/>.</summary>
        public Int64 TotalPages => Limit > 0 ? (Int64)Math.Ceiling(TotalItems / (Double)Limit) : 0;
    }
}
