namespace Connection360.Domain.Enums
{
    /// <summary>
    /// Identifica cada columna expuesta por las vistas de solo lectura
    /// connection360read.vw_application_data_sheet_entregados y
    /// connection360read.vw_application_data_sheet_no_entregados (ver
    /// Documents/vw_application_data_sheet_entregados.sql y
    /// Documents/vw_application_data_sheet_no_entregados.sql), ambas con exactamente las mismas
    /// columnas de connection360write.application_data_sheet (solo cambia el filtro por "estado").
    /// Se usa como "objeto parámetro" en
    /// <see cref="Connection360.Domain.Dtos.ApplicationDataSheetViewFieldsSelectionDto"/> para pedir
    /// un subconjunto de columnas en lugar de la fila completa.
    /// </summary>
    public enum ApplicationDataSheetViewField
    {
        Id,
        FechaCreacion,
        TipoOperacion,
        Modalidad,
        Incoterm,
        Proveedor,
        Cliente,
        NitCliente,
        Origen,
        Destino,
        DescripcionMercancia,
        Estado,
        TipoCarga,
        TipoContenedor,
        CantidadContenedores,
        NumeroContenedor,
        CantidadBultos,
        PesoKg,
        VolumenM3,
        Transportista,
        TipoDocumento,
        NombreDocumento,
        DocumentoTransporteHbl,
        FechaBodegaOrigen,
        FechaEtd,
        FechaAtd,
        FechaEta,
        FechaAta,
        FechaBodegaDestino,
        FechaNacionalizacion,
        FechaDespachoDestino,
        FechaPlanilla,
        FechaEntregaContenedor,
        FechaDevolucionRealContenedor,
        DiasLibres,
        DiasRestantesEntrega,
        DiasDemoraContenedor,
        ValorDiaDemora,
        ValorTotalDemora,
        DepositoContenedor,
        FechaSolicitudAnticipo,
        FechaPagoAnticipo,
        ValorAnticipo,
        FacturaProveedor,
        FacturaTcc,
        NumeroFactura,
        FechaFactura,
        DescripcionGasto,
        ValorGastoUsd,
        SubtotalFacturaUsd,
        IvaUsd,
        TotalFacturaUsd,
        Comentario,
        FechaComentario
    }
}
