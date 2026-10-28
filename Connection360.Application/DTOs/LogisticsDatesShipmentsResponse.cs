namespace Connection360.Application.DTOs
{
    /// <summary>
    /// Clase DTO para las fechas logisticas del envio
    /// </summary>
    public class LogisticsDatesShipmentsResponse
    {
        /// <summary>
        /// Fecha de bodega origen
        /// </summary>
        public DateTime StoreOriginDate { get; set; }

        /// <summary>
        /// Fecha estimada de salida
        /// </summary>
        public DateTime ETDDate { get; set; }

        /// <summary>
        /// Fecha Real de salida
        /// </summary>
        public DateTime ATDDate { get; set; }

        /// <summary>
        /// Fecha estimada de llegada
        /// </summary>
        public DateTime ETADate { get; set; }

        /// <summary>
        /// Fecha real de llegada
        /// </summary>
        public DateTime ATADate { get; set; }

        /// <summary>
        /// Fecha de bodega de destino
        /// </summary>
        public DateTime StoreDestinationDate { get; set; }

        /// <summary>
        /// Fecha de nacionalizacion
        /// </summary>
        public DateTime NationalizationDate { get; set; }

        /// <summary>
        /// Fecha de despacho destino
        /// </summary>
        public DateTime DispatchDestinationDate { get; set; }

        /// <summary>
        /// Fecha de planilla
        /// </summary>
        public DateTime FormDate { get; set; }

        /// <summary>
        /// Fecha de entrega contenedor
        /// </summary>
        public DateTime ContainerDeliveryDate { get; set; }
    }
}

