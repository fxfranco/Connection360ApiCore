namespace Connection360.Application.DTOs
{
    /// <summary>
    /// Clase DTO para el seguimiento del envio
    /// </summary>
    public class TrackingShipmentsResponse
    {
        /// <summary>
        /// Estado
        /// </summary>
        public String State { get; set; } = String.Empty;        

        /// <summary>
        /// Nombre del lugar de origen de las Coordenadas
        /// </summary>
        public String OriginNameCoordinates { get; set; } = String.Empty;

        /// <summary>
        /// Latitud del lugar de origen de las Coordenadas
        /// </summary>
        public String OriginLatitudCoordinates { get; set; } = String.Empty;

        /// <summary>
        /// Longitud del lugar de origen de las Coordenadas
        /// </summary>
        public String OriginLongitudCoordinates { get; set; } = String.Empty;


        /// <summary>
        /// Nombre del lugar de Destino de las Coordenadas
        /// </summary>
        public String DestinationNameCoordinates { get; set; } = String.Empty;

        /// <summary>
        /// Latitud del lugar de Destino de las Coordenadas
        /// </summary>
        public String DestinationLatitudCoordinates { get; set; } = String.Empty;

        /// <summary>
        /// Longitud del lugar de Destino de las Coordenadas
        /// </summary>
        public String DestinationLongitudCoordinates { get; set; } = String.Empty;
    }
}
