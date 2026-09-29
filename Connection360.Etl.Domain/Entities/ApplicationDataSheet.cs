using System;

namespace Connection360.Etl.Domain.Entities
{
    /// <summary>
    /// Fila de la bodega de datos (Data Warehouse): un registro por documento de transporte (HBL),
    /// con la información unificada de todas las APIs operativas. Corresponde 1:1 a la tabla
    /// PostgreSQL <c>connection360write.application_data_sheet</c> definida en
    /// Documents/scriptSabanDatosSQL.sql. La produce <see cref="Connection360.Etl.Domain.Services.ShipmentsDataSheetMappingService"/>
    /// (paso "Transform") a partir del <see cref="DynamicDataSet"/> unificado (BPMS+SIM+OPENCOMEX+ASISCOMEX+SYSTEMCARRIER)
    /// y la carga <see cref="Connection360.Etl.Domain.Ports.Persistence.IApplicationDataSheetRepository"/> (paso "Load").
    /// </summary>
    public sealed class ApplicationDataSheet
    {
        /// <summary>Identificador autogenerado por PostgreSQL (BIGSERIAL). No se envía al insertar/actualizar.</summary>
        public Int64 Id { get; set; }

        // ---------- Información general de operación ----------

        /// <summary>Fecha de creación del registro en el sistema origen.</summary>
        public DateTime FechaCreacion { get; set; }

        /// <summary>Tipo de operación: IMPO o EXPO.</summary>
        public String TipoOperacion { get; set; } = String.Empty;

        /// <summary>Modalidad de transporte: AIR o SEA.</summary>
        public String Modalidad { get; set; } = String.Empty;

        /// <summary>Incoterm de la operación.</summary>
        public String Incoterm { get; set; } = String.Empty;

        /// <summary>Proveedor.</summary>
        public String Proveedor { get; set; } = String.Empty;

        /// <summary>Nombre del cliente.</summary>
        public String Cliente { get; set; } = String.Empty;

        /// <summary>NIT del cliente.</summary>
        public String NitCliente { get; set; } = String.Empty;

        /// <summary>Origen del envío.</summary>
        public String Origen { get; set; } = String.Empty;

        /// <summary>Destino del envío.</summary>
        public String Destino { get; set; } = String.Empty;

        /// <summary>Descripción de la mercancía.</summary>
        public String DescripcionMercancia { get; set; } = String.Empty;

        /// <summary>Estado actual del envío.</summary>
        public String Estado { get; set; } = String.Empty;

        // ---------- Carga y contenedor ----------

        /// <summary>Tipo de carga: FCL o LCL.</summary>
        public String TipoCarga { get; set; } = String.Empty;

        /// <summary>Tipo de contenedor. Puede no aplicar (carga aérea, LCL, etc.).</summary>
        public String? TipoContenedor { get; set; }

        /// <summary>Cantidad de contenedores.</summary>
        public Int32 CantidadContenedores { get; set; }

        /// <summary>Número del contenedor. Puede no aplicar.</summary>
        public String? NumeroContenedor { get; set; }

        /// <summary>Cantidad de bultos.</summary>
        public Int32 CantidadBultos { get; set; }

        /// <summary>Peso en kilogramos.</summary>
        public Decimal PesoKg { get; set; }

        /// <summary>Volumen en metros cúbicos.</summary>
        public Decimal VolumenM3 { get; set; }

        // ---------- Transporte y documentación ----------

        /// <summary>Transportista.</summary>
        public String Transportista { get; set; } = String.Empty;

        /// <summary>Tipo de documento de transporte (código corto, ej. HBL/MBL).</summary>
        public String TipoDocumento { get; set; } = String.Empty;

        /// <summary>
        /// Nombre descriptivo del tipo de documento. Las APIs operativas actuales no exponen un
        /// campo propio para esto (solo <see cref="TipoDocumento"/>); mientras no exista una fuente
        /// específica, se usa el mismo valor de <see cref="TipoDocumento"/> como mejor aproximación.
        /// </summary>
        public String NombreDocumento { get; set; } = String.Empty;

        /// <summary>Número de documento de transporte (HBL). Llave única de negocio del registro.</summary>
        public String DocumentoTransporteHbl { get; set; } = String.Empty;

        // ---------- Hitos de fechas y tiempos ----------

        /// <summary>Fecha de ingreso a bodega de origen.</summary>
        public DateTime FechaBodegaOrigen { get; set; }

        /// <summary>Fecha estimada de salida (ETD).</summary>
        public DateTime FechaEtd { get; set; }

        /// <summary>Fecha real de salida (ATD).</summary>
        public DateTime FechaAtd { get; set; }

        /// <summary>Fecha estimada de llegada (ETA).</summary>
        public DateTime FechaEta { get; set; }

        /// <summary>Fecha real de llegada (ATA).</summary>
        public DateTime FechaAta { get; set; }

        /// <summary>Fecha de ingreso a bodega de destino.</summary>
        public DateTime FechaBodegaDestino { get; set; }

        /// <summary>Fecha de nacionalización.</summary>
        public DateTime FechaNacionalizacion { get; set; }

        /// <summary>Fecha de despacho en destino.</summary>
        public DateTime FechaDespachoDestino { get; set; }

        /// <summary>Fecha de planilla.</summary>
        public DateTime FechaPlanilla { get; set; }

        /// <summary>Fecha de entrega del contenedor.</summary>
        public DateTime FechaEntregaContenedor { get; set; }

        /// <summary>Fecha real de devolución del contenedor. Nulo mientras el contenedor no haya sido devuelto.</summary>
        public DateTime? FechaDevolucionRealContenedor { get; set; }

        // ---------- Gestión de demoras y libres ----------

        /// <summary>Días libres otorgados para el contenedor.</summary>
        public Int32 DiasLibres { get; set; }

        /// <summary>Días restantes para la entrega del contenedor.</summary>
        public Int32 DiasRestantesEntrega { get; set; }

        /// <summary>Días de demora del contenedor.</summary>
        public Int32 DiasDemoraContenedor { get; set; }

        /// <summary>Valor cobrado por cada día de demora.</summary>
        public Decimal ValorDiaDemora { get; set; }

        /// <summary>Valor total de la demora del contenedor.</summary>
        public Decimal ValorTotalDemora { get; set; }

        /// <summary>Valor del depósito del contenedor.</summary>
        public Decimal DepositoContenedor { get; set; }

        // ---------- Anticipos y facturación ----------

        /// <summary>Fecha de solicitud del anticipo.</summary>
        public DateTime FechaSolicitudAnticipo { get; set; }

        /// <summary>Fecha de pago del anticipo.</summary>
        public DateTime FechaPagoAnticipo { get; set; }

        /// <summary>Valor del anticipo.</summary>
        public Decimal ValorAnticipo { get; set; }

        /// <summary>Número de factura del proveedor.</summary>
        public String FacturaProveedor { get; set; } = String.Empty;

        /// <summary>Número de factura de TCC.</summary>
        public String FacturaTcc { get; set; } = String.Empty;

        /// <summary>Número de factura.</summary>
        public String NumeroFactura { get; set; } = String.Empty;

        /// <summary>Fecha de la factura.</summary>
        public DateTime FechaFactura { get; set; }

        /// <summary>Descripción del gasto facturado.</summary>
        public String DescripcionGasto { get; set; } = String.Empty;

        /// <summary>Valor del gasto en USD.</summary>
        public Decimal ValorGastoUsd { get; set; }

        /// <summary>Subtotal de la factura en USD.</summary>
        public Decimal SubtotalFacturaUsd { get; set; }

        /// <summary>IVA de la factura en USD.</summary>
        public Decimal IvaUsd { get; set; }

        /// <summary>Total de la factura en USD.</summary>
        public Decimal TotalFacturaUsd { get; set; }

        // ---------- Traza e historial ----------

        /// <summary>
        /// Mensaje del último cambio de estado registrado para este documento (tomado de la API
        /// DATALOGS). La tabla solo admite un comentario por registro, así que se usa el más
        /// reciente por <c>ID_LOG</c>, igual que el criterio de orden ya usado en
        /// Connection360.Domain.Services.DetailsHistoryShipmentsDomainService.
        /// </summary>
        public String Comentario { get; set; } = String.Empty;

        /// <summary>Fecha del último cambio de estado registrado (ver <see cref="Comentario"/>).</summary>
        public DateTime FechaComentario { get; set; }
    }
}
