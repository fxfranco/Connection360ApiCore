namespace Connection360.Application.DTOs
{
    /// <summary>
    /// Clase DTO para la información del contenedor
    /// </summary>
    public class ContainerShipmentsResponse
    {
        /// <summary>
        /// Tipo de contenedor
        /// </summary>
        public String ContainerType { get; set; } = String.Empty;

        /// <summary>
        /// Cantidad de contenedores
        /// </summary>
        public String ContainerAmount { get; set; } = String.Empty;

        /// <summary>
        /// Número de contenedor
        /// </summary>
        public String ContainerNumber { get; set; } = String.Empty;

        /// <summary>
        /// Días libres 
        /// </summary>
        public String DaysOff { get; set; } = String.Empty;

        /// <summary>
        /// Cantidad días restantes entrega contenedor
        /// </summary>
        public String DaysRemainingDelivery { get; set; } = String.Empty;

        /// <summary>
        /// Fecha de devolución real contenedor
        /// </summary>
        public DateTime ActualContainerReturnDate { get; set; }

        /// <summary>
        /// Cantidad dias demoras contenedor
        /// </summary>
        public String ContainerDelayDays { get; set; } = String.Empty;

        /// <summary>
        /// Valor por dia demoras contenedor
        /// </summary>
        public String CostDayOfDelay { get; set; } = String.Empty;

        /// <summary>
        /// Valor total demoras contenedor
        /// </summary>
        public String TotalCostContainerDelays { get; set; } = String.Empty;

        /// <summary>
        /// Deposito contenedor
        /// </summary>
        public String ContainerDepot { get; set; } = String.Empty;
    }
}
