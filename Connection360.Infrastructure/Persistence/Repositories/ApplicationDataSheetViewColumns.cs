using Connection360.Domain.Enums;

namespace Connection360.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Mapeo de <see cref="ApplicationDataSheetViewField"/> al fragmento SQL de cada columna,
    /// compartido por <see cref="ApplicationDataSheetEntregadosRepository"/> y
    /// <see cref="ApplicationDataSheetNoEntregadosRepository"/> (ambas vistas tienen exactamente las
    /// mismas columnas; ver Documents/vw_application_data_sheet_entregados.sql y
    /// Documents/vw_application_data_sheet_no_entregados.sql). No hacen falta alias "AS" porque
    /// Connection360.Api/Program.cs habilita Dapper.DefaultTypeMap.MatchNamesWithUnderscores: el
    /// mapeo a cada propiedad PascalCase de ApplicationDataSheetViewResultDto es automático. Las
    /// columnas DATE llevan cast ::timestamp porque Npgsql 10 las devuelve por defecto como
    /// System.DateOnly, que Dapper no puede mapear automáticamente a una propiedad DateTime (mismo
    /// ajuste ya aplicado en Connection360.Etl.Infrastructure.Persistence.Repositories.ApplicationDataSheetRepository).
    /// </summary>
    internal static class ApplicationDataSheetViewColumns
    {
        private static readonly (ApplicationDataSheetViewField Field, String Sql)[] _columns =
        {
            (ApplicationDataSheetViewField.Id, "id"),
            (ApplicationDataSheetViewField.FechaCreacion, "fecha_creacion::timestamp"),
            (ApplicationDataSheetViewField.TipoOperacion, "tipo_operacion"),
            (ApplicationDataSheetViewField.Modalidad, "modalidad"),
            (ApplicationDataSheetViewField.Incoterm, "incoterm"),
            (ApplicationDataSheetViewField.Proveedor, "proveedor"),
            (ApplicationDataSheetViewField.Cliente, "cliente"),
            (ApplicationDataSheetViewField.NitCliente, "nit_cliente"),
            (ApplicationDataSheetViewField.Origen, "origen"),
            (ApplicationDataSheetViewField.Destino, "destino"),
            (ApplicationDataSheetViewField.DescripcionMercancia, "descripcion_mercancia"),
            (ApplicationDataSheetViewField.Estado, "estado"),
            (ApplicationDataSheetViewField.TipoCarga, "tipo_carga"),
            (ApplicationDataSheetViewField.TipoContenedor, "tipo_contenedor"),
            (ApplicationDataSheetViewField.CantidadContenedores, "cantidad_contenedores"),
            (ApplicationDataSheetViewField.NumeroContenedor, "numero_contenedor"),
            (ApplicationDataSheetViewField.CantidadBultos, "cantidad_bultos"),
            (ApplicationDataSheetViewField.PesoKg, "peso_kg"),
            (ApplicationDataSheetViewField.VolumenM3, "volumen_m3"),
            (ApplicationDataSheetViewField.Transportista, "transportista"),
            (ApplicationDataSheetViewField.TipoDocumento, "tipo_documento"),
            (ApplicationDataSheetViewField.NombreDocumento, "nombre_documento"),
            (ApplicationDataSheetViewField.DocumentoTransporteHbl, "documento_transporte_hbl"),
            (ApplicationDataSheetViewField.FechaBodegaOrigen, "fecha_bodega_origen::timestamp"),
            (ApplicationDataSheetViewField.FechaEtd, "fecha_etd::timestamp"),
            (ApplicationDataSheetViewField.FechaAtd, "fecha_atd::timestamp"),
            (ApplicationDataSheetViewField.FechaEta, "fecha_eta::timestamp"),
            (ApplicationDataSheetViewField.FechaAta, "fecha_ata::timestamp"),
            (ApplicationDataSheetViewField.FechaBodegaDestino, "fecha_bodega_destino::timestamp"),
            (ApplicationDataSheetViewField.FechaNacionalizacion, "fecha_nacionalizacion::timestamp"),
            (ApplicationDataSheetViewField.FechaDespachoDestino, "fecha_despacho_destino::timestamp"),
            (ApplicationDataSheetViewField.FechaPlanilla, "fecha_planilla::timestamp"),
            (ApplicationDataSheetViewField.FechaEntregaContenedor, "fecha_entrega_contenedor::timestamp"),
            (ApplicationDataSheetViewField.FechaDevolucionRealContenedor, "fecha_devolucion_real_contenedor::timestamp"),
            (ApplicationDataSheetViewField.DiasLibres, "dias_libres"),
            (ApplicationDataSheetViewField.DiasRestantesEntrega, "dias_restantes_entrega"),
            (ApplicationDataSheetViewField.DiasDemoraContenedor, "dias_demora_contenedor"),
            (ApplicationDataSheetViewField.ValorDiaDemora, "valor_dia_demora"),
            (ApplicationDataSheetViewField.ValorTotalDemora, "valor_total_demora"),
            (ApplicationDataSheetViewField.DepositoContenedor, "deposito_contenedor"),
            (ApplicationDataSheetViewField.FechaSolicitudAnticipo, "fecha_solicitud_anticipo::timestamp"),
            (ApplicationDataSheetViewField.FechaPagoAnticipo, "fecha_pago_anticipo::timestamp"),
            (ApplicationDataSheetViewField.ValorAnticipo, "valor_anticipo"),
            (ApplicationDataSheetViewField.FacturaProveedor, "factura_proveedor"),
            (ApplicationDataSheetViewField.FacturaTcc, "factura_tcc"),
            (ApplicationDataSheetViewField.NumeroFactura, "numero_factura"),
            (ApplicationDataSheetViewField.FechaFactura, "fecha_factura::timestamp"),
            (ApplicationDataSheetViewField.DescripcionGasto, "descripcion_gasto"),
            (ApplicationDataSheetViewField.ValorGastoUsd, "valor_gasto_usd"),
            (ApplicationDataSheetViewField.SubtotalFacturaUsd, "subtotal_factura_usd"),
            (ApplicationDataSheetViewField.IvaUsd, "iva_usd"),
            (ApplicationDataSheetViewField.TotalFacturaUsd, "total_factura_usd"),
            (ApplicationDataSheetViewField.Comentario, "comentario"),
            (ApplicationDataSheetViewField.FechaComentario, "fecha_comentario::timestamp"),
        };

        /// <summary>Lista completa de columnas, en el mismo orden de las vistas (equivalente a SELECT * en forma explícita).</summary>
        public static readonly String AllColumnsSql = String.Join(", ", _columns.Select(c => c.Sql));

        /// <summary>
        /// Construye la lista de columnas del SELECT a partir de un subconjunto de campos pedido.
        /// Conserva siempre el orden fijo de la vista (no el orden en el que llegaron en
        /// <paramref name="fields"/>) y descarta duplicados.
        /// </summary>
        public static String BuildSelectColumns(IEnumerable<ApplicationDataSheetViewField> fields)
        {
            HashSet<ApplicationDataSheetViewField> requested = fields is null
                ? new HashSet<ApplicationDataSheetViewField>()
                : new HashSet<ApplicationDataSheetViewField>(fields);

            if (requested.Count == 0)
            {
                throw new ArgumentException(
                    "ApplicationDataSheetViewFieldsSelectionDto.Fields debe traer al menos un campo.",
                    nameof(fields));
            }

            List<String> selected = _columns.Where(c => requested.Contains(c.Field)).Select(c => c.Sql).ToList();
            return String.Join(", ", selected);
        }
    }
}
