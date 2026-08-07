namespace Connection360.Domain.Constans
{
    /// <summary>
    /// Nombre de los campos (columnas) de la base de datos que expone las apis externas - centralizados para evitar "magic strings"
    /// </summary>
    public static class ExternalDataFields
    {
        /// <summary>
        /// ID del registro de la base de datos
        /// </summary>
        public const String ID = "ID";
        
        /// <summary>
        /// Nit del cliente
        /// </summary>
        public const String ClientNit = "NIT CLIENTE";
        
        /// <summary>
        /// Nombre del cliente
        /// </summary>
        public const String ClientName = "CLIENTE";

        /// <summary>
        /// Tipo de operacion (IMPO - EXPO)
        /// </summary>
        public const String OperationType = "TIPO DE OPERACIÓN (IMPO - EXPO)";
        
        /// <summary>
        /// Tipo de modalidad (AIR - SEA)
        /// </summary>
        public const String ShipmentMode = "MODALIDAD (AIR - SEA)";
        
        /// <summary>
        /// Fecha de creación
        /// </summary>
        public const String CreationDate = "FECHA DE CREACIÓN";
        
        /// <summary>
        /// Estado
        /// </summary>
        public const String State = "ESTADO";
        
        /// <summary>
        /// Documento de transforte (Guia)
        /// </summary>
        public const String DocumentNumber = "DOCUMENTO DE TRANSPORTE (HBL)";
        
        /// <summary>
        /// Origen
        /// </summary>
        public const String Origin = "ORIGEN";
        
        /// <summary>
        /// Destino
        /// </summary>
        public const String Destination = "DESTINO";
        
        /// <summary>
        /// ETD - Fecha estimada de salida
        /// </summary>
        public const String ETDDate = "ETD (Fecha Estimada Salida)";

        /// <summary>
        /// ATD - Fecha real de salida
        /// </summary>
        public const String ATDDate = "ATD (Fecha Real Salida)";

        /// <summary>
        /// ETA - Fecha estimada de llegada
        /// </summary>
        public const String ETADate = "ETA (Fecha Estimada Llegada)";

        /// <summary>
        /// ATA - Fecha real de llegada
        /// </summary>
        public const String ATADate = "ATA (Fecha Real Llegada)";
    }
}
