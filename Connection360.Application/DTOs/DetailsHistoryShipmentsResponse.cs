namespace Connection360.Application.DTOs
{
    /// <summary>
    /// Clase DTO para el detalle del historial de los envios
    /// </summary>
    public class DetailsHistoryShipmentsResponse
    {
        /// <summary>
        /// Fecha que se hizo el cambio
        /// </summary>
        public DateTime ChangeDate { get; set; }

        /// <summary>
        /// Usarios que hizo el cambio
        /// </summary>
        public String ChangeUser { get; set; } = String.Empty;

        /// <summary>
        /// Descripciòn/mensaje del cambio
        /// </summary>
        public String Message { get; set; } = String.Empty;

        /// <summary>
        /// Estado anterior
        /// </summary>
        public String OldState { get; set; } = String.Empty;

        /// <summary>
        /// Estado nuevo
        /// </summary>
        public String NewState { get; set; } = String.Empty;
    }
}
