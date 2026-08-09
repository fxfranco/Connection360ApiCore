namespace Connection360.Application.DTOs
{
    /// <summary>
    /// Clase DTO para el resumen de informacion principal del envio
    /// </summary>
    public class SummaryShipmentsResponse
    {
        /// <summary>
        /// Id de la base de datos
        /// </summary>
        public String Id { get; set; } = String.Empty;

        /// <summary>
        /// Nombre del cliente
        /// </summary>
        public String ClientName { get; set; } = String.Empty;
        
        /// <summary>
        /// Proveedor
        /// </summary>
        public String Supplier { get; set; } = String.Empty;

        /// <summary>
        /// Transportista
        /// </summary>
        public String Carrier { get; set; } = String.Empty;
        
        /// <summary>
        /// Descripciòn de la mercancia
        /// </summary>
        public String MerchandiseDescription { get; set; } = String.Empty;
        
        /// <summary>
        /// Nùmero de documento
        /// </summary>
        public String DocumentNumber { get; set; } = String.Empty;
        
        /// <summary>
        /// Tipo de documento
        /// </summary>
        public String DocumentType { get; set; } = String.Empty;
        
        /// <summary>
        /// Origen
        /// </summary>
        public String Origin { get; set; } = String.Empty;
        
        /// <summary>
        /// Destino
        /// </summary>
        public String Destination { get; set; } = String.Empty;
        
        /// <summary>
        /// Tipo de carga
        /// </summary>
        public String LoadType { get; set; } = String.Empty;
        
        /// <summary>
        /// Cantidad de bultos
        /// </summary>
        public String PackagesNumbers { get; set; } = String.Empty;
        
        /// <summary>
        /// Peso
        /// </summary>
        public String WeightKg { get; set; } = String.Empty;
        
        /// <summary>
        /// Volumen
        /// </summary>
        public String VolumeM3 { get; set; } = String.Empty;
        
        /// <summary>
        /// Incoterm
        /// </summary>
        public String Incoterm { get; set; } = String.Empty;
        
        /// <summary>
        /// Tipo de operación
        /// </summary>
        public String OperationType { get; set; } = String.Empty;
        
        /// <summary>
        /// Modalidad
        /// </summary>
        public String ShipmentMode { get; set; } = String.Empty;

    }
}
