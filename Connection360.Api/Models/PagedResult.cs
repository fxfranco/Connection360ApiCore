namespace Connection360.Api.Models
{
    /// <summary>
    /// Resultado paginado producido internamente por los controladores antes de que
    /// <see cref="Connection360.Api.Filters.ApiResponseFilter"/> lo transforme en la respuesta final:
    /// el filtro mueve <see cref="Items"/> a <c>ApiResponse&lt;T&gt;.DataResponse</c> y el resto de
    /// propiedades a <c>ApiResponse&lt;T&gt;.Meta</c>. No es, por lo tanto, la forma final del JSON
    /// que recibe el cliente de la API.
    /// </summary>
    /// <typeparam name="T">Tipo de cada elemento de la página.</typeparam>
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
