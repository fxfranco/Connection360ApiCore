namespace Connection360.Api.Models
{
    /// <summary>
    /// Metadatos de paginación que acompañan a <see cref="ApiResponse{T}"/> cuando el dato devuelto
    /// (<c>DataResponse</c>) es un listado paginado.
    /// </summary>
    public class MetaResponse
    {
        /// <summary>Cantidad total de elementos disponibles (sin paginar) que cumplen el criterio de la consulta.</summary>
        public Int64 TotalItems { get; set; }

        /// <summary>Cantidad total de páginas resultantes según <see cref="TotalItems"/> y <see cref="Limit"/>.</summary>
        public Int64 TotalPages { get; set; }

        /// <summary>Número de la página actual (según el parámetro de paginación recibido en la solicitud).</summary>
        public Int64 CurrentPage { get; set; }

        /// <summary>Cantidad máxima de elementos incluidos por página.</summary>
        public Int64 Limit { get; set; }
    }
}
