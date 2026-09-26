namespace Connection360.Application.DTOs
{
    /// <summary>
    /// Filtros opcionales para acotar el listado de "mis envíos" o su historial (ver
    /// <c>MyShipmentsController.FilterShipments</c>/<c>FilterHistoryShipments</c>). Se recibe
    /// directamente de la query string del cliente HTTP.
    /// </summary>
    public class MyShipmentsFiltersRequest
    {
        /// <summary>Valor de texto libre a buscar (por ejemplo, número de documento o cliente).</summary>
        public String? ValueFilter { get; set; }

        /// <summary>Filtra por tipo de operación (importación/exportación).</summary>
        public String? OperationType { get; set; }

        /// <summary>Filtra por modalidad de transporte (aéreo/marítimo).</summary>
        public String? ShipmentMode { get; set; }

        /// <summary>Filtra por estado del envío.</summary>
        public String? State { get; set; }
    }
}
