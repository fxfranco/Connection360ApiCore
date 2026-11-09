namespace Connection360.Domain.Constans
{
    /// <summary>
    /// Nombre de los valores (registros) de la base de datos con tipo opción - lista (enumeracion) que expone las apis externas - centralizados para evitar "magic strings"
    /// </summary>
    public static class ExternalDataValues
    {
        #region Tipo de operacion
        /// <summary>
        /// Importaciones
        /// </summary>
        public const String Import = "IMPO";
        
        /// <summary>
        /// Exportaciones
        /// </summary>
        public const String Export = "EXPO";

        #endregion

        #region Tipo de modalidad
        /// <summary>
        /// Modalidad Aerea
        /// </summary>
        public const String AirShipment = "AIR";
        
        /// <summary>
        /// Modalidad Maritima
        /// </summary>
        public const String OceanShipment = "SEA";

        #endregion

        #region Estados
        /// <summary>
        /// Estado "Con novedad"
        /// </summary>
        public const String WithIssuesState = "Con novedad";
        
        /// <summary>
        /// Estado "Entregado"
        /// </summary>
        public const String DeliveredState = "Entregado";

        /// <summary>
        /// Estado "En Aduana destino"
        /// </summary>
        public const String DestinationCustomsState = "En Aduana destino";

        /// <summary>
        /// Estado "En Aduana origen"
        /// </summary>
        public const String OriginCustomsState = "En Aduana origen";

        /// <summary>
        /// Estado "En tránsito"
        /// </summary>
        public const String InTransitState = "En tránsito";

        /// <summary>
        /// Estado "Pendiente"
        /// </summary>
        public const String PendingState = "Pendiente";

        #endregion
    }
}
