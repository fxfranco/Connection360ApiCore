namespace Connection360.Domain.Dtos
{
    /// <summary>
    /// Clase DTO para la información financiera del envio
    /// </summary>
    public class FinancialInfoShipmentsDomainDtoResult
    {
        /// <summary>
        /// Fecha solicitud anticipo
        /// </summary>
        public DateTime AdvancePaymentRequestDate { get; set; }

        /// <summary>
        /// Fecha pago anticipo
        /// </summary>
        public DateTime AdvancePaymentDate { get; set; }

        /// <summary>
        /// Valor anticipo
        /// </summary>
        public String AdvancePaymentAmount { get; set; } = String.Empty;

        /// <summary>
        /// Factura proveedor
        /// </summary>
        public String SupplierInvoice { get; set; } = String.Empty;

        /// <summary>
        /// Factura TCC
        /// </summary>
        public String TCCInvoice { get; set; } = String.Empty;

        /// <summary>
        /// Numero de factura
        /// </summary>
        public String InvoiceNumber { get; set; } = String.Empty;

        /// <summary>
        /// Fecha de factura
        /// </summary>
        public String InvoiceDate { get; set; } = String.Empty;

        /// <summary>
        /// Descripcion gasto
        /// </summary>
        public String ExpenseDescription { get; set; } = String.Empty;

        /// <summary>
        /// Valor gasto USD
        /// </summary>
        public String ExpenseAmountUSD { get; set; } = String.Empty;

        /// <summary>
        /// Subtotal factura USD
        /// </summary>
        public String InvoiceSubtotalUSD { get; set; } = String.Empty;

        /// <summary>
        /// Iva USD
        /// </summary>
        public String IvaUSD { get; set; } = String.Empty;

        /// <summary>
        /// Total factura USD
        /// </summary>
        public String TotalInvoiceUSD { get; set; } = String.Empty;
    }
}
