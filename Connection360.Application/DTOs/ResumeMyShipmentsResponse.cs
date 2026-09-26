using System;
using System.Collections.Generic;
using System.Text;

namespace Connection360.Application.DTOs
{
    /// <summary>
    /// Resumen de un envío individual dentro del listado de "mis envíos" (ver
    /// <see cref="ClientSummaryResponse.MyShipments"/>).
    /// </summary>
    public class ResumeMyShipmentsResponse
    {
        /// <summary>Identificador interno del registro.</summary>
        public Int64 Id { get; set; }

        /// <summary>NIT del cliente.</summary>
        public String ClientNit { get; set; } = String.Empty;

        /// <summary>Modalidad de transporte del envío (aéreo/marítimo).</summary>
        public String ShipmentMode { get; set; } = String.Empty;

        /// <summary>Número de documento del envío.</summary>
        public String DocumentNumber { get; set; } = String.Empty;

        /// <summary>Estado actual del envío.</summary>
        public String State { get; set; } = String.Empty;

        /// <summary>Tipo de operación (importación/exportación).</summary>
        public String OperationType { get; set; } = String.Empty;

        /// <summary>Nombre del cliente.</summary>
        public String ClientName { get; set; } = String.Empty;

        /// <summary>Lugar de origen del envío.</summary>
        public String Origin { get; set; } = String.Empty;

        /// <summary>Lugar de destino del envío.</summary>
        public String Destination { get; set; } = String.Empty;

        /// <summary>Fecha estimada de salida.</summary>
        public DateTime ETDDate { get; set; }

        /// <summary>Fecha real de salida.</summary>
        public DateTime ATDDate { get; set; }

        /// <summary>Fecha estimada de llegada.</summary>
        public DateTime ETADate { get; set; }

        /// <summary>Fecha real de llegada.</summary>
        public DateTime ATADate { get; set; }
    }
}
