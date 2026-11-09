namespace Connection360.Domain.Dtos
{
    /// <summary>
    /// Fila de las vistas de solo lectura connection360read.vw_application_data_sheet_entregados /
    /// vw_application_data_sheet_no_entregados (ver Documents/vw_application_data_sheet_entregados.sql
    /// y Documents/vw_application_data_sheet_no_entregados.sql). Ambas vistas exponen exactamente
    /// las mismas columnas de connection360write.application_data_sheet (solo cambia el filtro por
    /// "estado"), así que se usa un único Dto para el resultado de los dos repositorios
    /// (IApplicationDataSheetEntregadosRepository / IApplicationDataSheetNoEntregadosRepository).
    /// Cuando se piden solo algunos campos (ver <see cref="ApplicationDataSheetViewFieldsSelectionDto"/>),
    /// las propiedades no seleccionadas quedan con su valor por defecto.
    /// </summary>
    public class ApplicationDataSheetViewResultDto
    {
        public Int64 Id { get; set; }
        public DateTime FechaCreacion { get; set; }
        public String TipoOperacion { get; set; } = String.Empty;
        public String Modalidad { get; set; } = String.Empty;
        public String Incoterm { get; set; } = String.Empty;
        public String Proveedor { get; set; } = String.Empty;
        public String Cliente { get; set; } = String.Empty;
        public String NitCliente { get; set; } = String.Empty;
        public String Origen { get; set; } = String.Empty;
        public String Destino { get; set; } = String.Empty;
        public String DescripcionMercancia { get; set; } = String.Empty;
        public String Estado { get; set; } = String.Empty;
        public String TipoCarga { get; set; } = String.Empty;
        public String? TipoContenedor { get; set; }
        public Int32 CantidadContenedores { get; set; }
        public String? NumeroContenedor { get; set; }
        public Int32 CantidadBultos { get; set; }
        public Decimal PesoKg { get; set; }
        public Decimal VolumenM3 { get; set; }
        public String Transportista { get; set; } = String.Empty;
        public String TipoDocumento { get; set; } = String.Empty;
        public String NombreDocumento { get; set; } = String.Empty;
        public String DocumentoTransporteHbl { get; set; } = String.Empty;
        public DateTime FechaBodegaOrigen { get; set; }
        public DateTime FechaEtd { get; set; }
        public DateTime FechaAtd { get; set; }
        public DateTime FechaEta { get; set; }
        public DateTime FechaAta { get; set; }
        public DateTime FechaBodegaDestino { get; set; }
        public DateTime FechaNacionalizacion { get; set; }
        public DateTime FechaDespachoDestino { get; set; }
        public DateTime FechaPlanilla { get; set; }
        public DateTime FechaEntregaContenedor { get; set; }
        public DateTime? FechaDevolucionRealContenedor { get; set; }
        public Int32 DiasLibres { get; set; }
        public Int32 DiasRestantesEntrega { get; set; }
        public Int32 DiasDemoraContenedor { get; set; }
        public Decimal ValorDiaDemora { get; set; }
        public Decimal ValorTotalDemora { get; set; }
        public Decimal DepositoContenedor { get; set; }
        public DateTime FechaSolicitudAnticipo { get; set; }
        public DateTime FechaPagoAnticipo { get; set; }
        public Decimal ValorAnticipo { get; set; }
        public String FacturaProveedor { get; set; } = String.Empty;
        public String FacturaTcc { get; set; } = String.Empty;
        public String NumeroFactura { get; set; } = String.Empty;
        public DateTime FechaFactura { get; set; }
        public String DescripcionGasto { get; set; } = String.Empty;
        public Decimal ValorGastoUsd { get; set; }
        public Decimal SubtotalFacturaUsd { get; set; }
        public Decimal IvaUsd { get; set; }
        public Decimal TotalFacturaUsd { get; set; }
        public String Comentario { get; set; } = String.Empty;
        public DateTime FechaComentario { get; set; }
    }
}
