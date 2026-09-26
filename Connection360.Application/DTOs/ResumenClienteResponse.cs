namespace Connection360.Application.DTOs
{
    /// <summary>
    /// Resumen de un envío individual de un cliente, usado en los listados de envíos recientes
    /// (ver <see cref="ClientSummaryResponse.RecentShipments"/>) devueltos por
    /// <c>HomeController.GetHomeFilters</c>.
    /// </summary>
    public class ResumenClienteResponse
    {
        /// <summary>Identificador interno del registro.</summary>
        public Int64 Id { get; set; }

        /// <summary>NIT del cliente.</summary>
        public String ClientNit { get; set; } = String.Empty;

        /// <summary>Nombre del cliente.</summary>
        public String ClientName { get; set; } = String.Empty;

        /// <summary>Número de documento del envío.</summary>
        public String DocumentNumber { get; set; } = String.Empty;

        /// <summary>Lugar de origen del envío.</summary>
        public String Origin { get; set; } = String.Empty;

        /// <summary>Lugar de destino del envío.</summary>
        public String Destination { get; set; } = String.Empty;

        /// <summary>Estado actual del envío.</summary>
        public String Status { get; set; } = String.Empty;

        /// <summary>Tipo de operación (importación/exportación).</summary>
        public String OperationType { get; set; } = String.Empty;

        /// <summary>Modalidad de transporte del envío (aéreo/marítimo).</summary>
        public String ShipmentMode { get; set; } = String.Empty;
    }
}
