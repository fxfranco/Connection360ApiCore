namespace Connection360Notification.Api.Models
{
    /// <summary>
    /// Metadatos de paginación incluidos en <see cref="ApiResponse{T}.Meta"/> cuando la respuesta original es un <see cref="PagedResult{T}"/>.
    /// </summary>
    public class MetaResponse
    {
        /// <summary>Cantidad total de elementos disponibles (sin paginar).</summary>
        public Int64 TotalItems { get; set; }

        /// <summary>Cantidad total de páginas resultantes según <see cref="Limit"/>.</summary>
        public Int64 TotalPages { get; set; }

        /// <summary>Número de la página actual.</summary>
        public Int64 CurrentPage { get; set; }

        /// <summary>Cantidad máxima de elementos por página.</summary>
        public Int64 Limit { get; set; }
    }
}
