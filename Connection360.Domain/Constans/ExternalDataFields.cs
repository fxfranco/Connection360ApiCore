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
        /// Tipo de documento (prefijo del la guia)
        /// </summary>
        public const String DocumentType = "TIPO DOCUMENTO";
        
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

        /// <summary>
        /// Proveedor
        /// </summary>
        public const String Supplier = "PROVEEDOR";

        /// <summary>
        /// Transportista
        /// </summary>
        public const String Carrier = "TRANSPORTISTA";

        /// <summary>
        /// Descripciòn de la mercancia
        /// </summary>
        public const String MerchandiseDescription = "DESCRIPCIÓN DE LA MERCANCÍA";

        /// <summary>
        /// Tipo de carga
        /// </summary>
        public const String LoadType = "TIPO DE CARGA (FCL - LCL)";

        /// <summary>
        /// Cantidad de bultos
        /// </summary>
        public const String PackagesNumbers = "CANTIDAD BULTOS";

        /// <summary>
        /// Peso (kg)
        /// </summary>
        public const String WeightKg = "PESO (kg)";

        /// <summary>
        /// Volumen (m3)
        /// </summary>
        public const String VolumeM3 = "VOLUMEN (m3)";

        /// <summary>
        /// Incoterm
        /// </summary>
        public const String Incoterm = "INCOTERM";

        /// <summary>
        /// Fecha bodega de origen
        /// </summary>
        public const String StoreOriginDate = "FECHA BODEGA ORIGEN";

        /// <summary>
        /// Fecha bodega de destino
        /// </summary>
        public const String StoreDestinationDate = "FECHA BODEGA DESTINO";

        /// <summary>
        /// Fecha de nacionalizacion
        /// </summary>
        public const String NationalizationDate = "FECHA DE NACIONALIZACION";

        /// <summary>
        /// Fecha de despacho (destino)
        /// </summary>
        public const String DispatchDestinationDate = "FECHA DESPACHO (DESTINO)";

        /// <summary>
        /// Fecha de planilla
        /// </summary>
        public const String FormDate = "FECHA PLANILLA";

        /// <summary>
        /// Fecha de entrega contenedor
        /// </summary>
        public const String ContainerDeliveryDate = "FECHA ENTREGA CONTENEDOR";

        /// <summary>
        /// tipo de contenedor
        /// </summary>
        public const String ContainerType = "TIPO DE CONTENEDOR";

        /// <summary>
        /// Cantidad de contenedores
        /// </summary>
        public const String ContainerAmount = "CANTIDAD CONTENEDORES";

        /// <summary>
        /// Numero de contenedores
        /// </summary>
        public const String ContainerNumber = "NÚMERO CONTENEDOR";

        /// <summary>
        /// Días libres
        /// </summary>
        public const String DaysOff = "DIAS LIBRES";

        /// <summary>
        /// Cantidad dias restantes entrega contenedor
        /// </summary>
        public const String DaysRemainingDelivery = "CANT. DIAS RESTANTES ENTREGA CONTENEDOR";

        /// <summary>
        /// Fecha de devoluciòn real contenedor
        /// </summary>
        public const String ActualContainerReturnDate = "FECHA DE DEVOLUCIÓN REAL CONTENEDOR";

        /// <summary>
        /// Cantidad dias demoras contenedor
        /// </summary>
        public const String ContainerDelayDays = "CANT. DIAS DEMORAS CONTENEDOR";

        /// <summary>
        /// Valor por dia demoras contenedor
        /// </summary>
        public const String CostDayOfDelay = "VALOR POR DIA DEMORAS CONTENEDOR";

        /// <summary>
        /// Valor total demoras contenedor
        /// </summary>
        public const String TotalCostContainerDelays = "VALOR TOTAL DEMORAS CONTENEDOR";

        /// <summary>
        /// Deposito Contenedor
        /// </summary>
        public const String ContainerDepot = "DEPOSITO CONTEDOR";

        /// <summary>
        /// Fecha solicitud anticipio
        /// </summary>
        public const String AdvancePaymentRequestDate = "FECHA SOLICITUD ANTICIPO";

        /// <summary>
        /// Fecha pago anticipo
        /// </summary>
        public const String AdvancePaymentDate = "FECHA PAGO ANTICIPO";

        /// <summary>
        /// Valor anticipo
        /// </summary>
        public const String AdvancePaymentAmount = "VALOR ANTICIPO";

        /// <summary>
        /// Factura proveedor
        /// </summary>
        public const String SupplierInvoice = "FACTURA PROVEEDOR";

        /// <summary>
        /// Factura TCC
        /// </summary>
        public const String TCCInvoice = "FACTURA TCC";

        /// <summary>
        /// Nùmero de factura
        /// </summary>
        public const String InvoiceNumber = "NÚMERO FACTURA";

        /// <summary>
        /// Fecha de factura
        /// </summary>
        public const String InvoiceDate = "FECHA FACTURA";

        /// <summary>
        /// Descripciòn gasto
        /// </summary>
        public const String ExpenseDescription = "DESCRIPCIÓN GASTO";

        /// <summary>
        /// Valor Gasto (USD)
        /// </summary>
        public const String ExpenseAmountUSD = "VALOR GASTO (USD)";

        /// <summary>
        /// Subtotal Factura (usd)
        /// </summary>
        public const String InvoiceSubtotalUSD = "SUBTOTAL FACTURA (USD)";

        /// <summary>
        /// Iva (USD)
        /// </summary>
        public const String IvaUSD = "IVA (USD)";

        /// <summary>
        /// Total Factura (USD)
        /// </summary>
        public const String TotalInvoiceUSD = "TOTAL FACTURA (USD)";

    }
}
