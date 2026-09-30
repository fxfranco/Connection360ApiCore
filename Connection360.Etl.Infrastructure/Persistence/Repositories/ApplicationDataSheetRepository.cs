using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Domain.Ports.Persistence;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Connection360.Etl.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Adaptador del paso "Load": inserta/actualiza (upsert) filas de la bodega de datos en
    /// PostgreSQL, tabla connection360write.application_data_sheet (ver Documents/scriptSabanDatosSQL.sql).
    /// Sigue el mismo patrón Dapper + <see cref="DbSession"/> que el resto de repositorios de
    /// Connection360.Infrastructure.Persistence.Repositories (ver por ejemplo MasterSettingsRepository).
    /// </summary>
    public class ApplicationDataSheetRepository : IApplicationDataSheetRepository
    {
        private readonly DbSession _session;

        public ApplicationDataSheetRepository(DbSession session)
        {
            _session = session;
        }

        public async Task<Int32> UpsertBatchAsync(IEnumerable<ApplicationDataSheet> rows, CancellationToken cancellationToken = default)
        {
            var rowsList = rows?.ToList() ?? new List<ApplicationDataSheet>();
            if (rowsList.Count == 0)
                return 0;

            await _session.EnsureConnectionOpenAsync(cancellationToken);

            // documento_transporte_hbl es UNIQUE en la tabla (ver script SQL), por lo que se usa
            // como llave de conflicto: reprocesar el mismo período es idempotente (no duplica filas,
            // solo actualiza los valores más recientes de cada documento).
            const string query = @"
                INSERT INTO connection360write.application_data_sheet
                    (fecha_creacion, tipo_operacion, modalidad, incoterm, proveedor, cliente, nit_cliente,
                     origen, destino, descripcion_mercancia, estado,
                     tipo_carga, tipo_contenedor, cantidad_contenedores, numero_contenedor, cantidad_bultos, peso_kg, volumen_m3,
                     transportista, tipo_documento, nombre_documento, documento_transporte_hbl,
                     fecha_bodega_origen, fecha_etd, fecha_atd, fecha_eta, fecha_ata, fecha_bodega_destino,
                     fecha_nacionalizacion, fecha_despacho_destino, fecha_planilla, fecha_entrega_contenedor, fecha_devolucion_real_contenedor,
                     dias_libres, dias_restantes_entrega, dias_demora_contenedor, valor_dia_demora, valor_total_demora, deposito_contenedor,
                     fecha_solicitud_anticipo, fecha_pago_anticipo, valor_anticipo, factura_proveedor, factura_tcc, numero_factura,
                     fecha_factura, descripcion_gasto, valor_gasto_usd, subtotal_factura_usd, iva_usd, total_factura_usd,
                     comentario, fecha_comentario)
                VALUES
                    (@FechaCreacion, @TipoOperacion, @Modalidad, @Incoterm, @Proveedor, @Cliente, @NitCliente,
                     @Origen, @Destino, @DescripcionMercancia, @Estado,
                     @TipoCarga, @TipoContenedor, @CantidadContenedores, @NumeroContenedor, @CantidadBultos, @PesoKg, @VolumenM3,
                     @Transportista, @TipoDocumento, @NombreDocumento, @DocumentoTransporteHbl,
                     @FechaBodegaOrigen, @FechaEtd, @FechaAtd, @FechaEta, @FechaAta, @FechaBodegaDestino,
                     @FechaNacionalizacion, @FechaDespachoDestino, @FechaPlanilla, @FechaEntregaContenedor, @FechaDevolucionRealContenedor,
                     @DiasLibres, @DiasRestantesEntrega, @DiasDemoraContenedor, @ValorDiaDemora, @ValorTotalDemora, @DepositoContenedor,
                     @FechaSolicitudAnticipo, @FechaPagoAnticipo, @ValorAnticipo, @FacturaProveedor, @FacturaTcc, @NumeroFactura,
                     @FechaFactura, @DescripcionGasto, @ValorGastoUsd, @SubtotalFacturaUsd, @IvaUsd, @TotalFacturaUsd,
                     @Comentario, @FechaComentario)
                ON CONFLICT (documento_transporte_hbl) DO UPDATE SET
                     fecha_creacion = EXCLUDED.fecha_creacion,
                     tipo_operacion = EXCLUDED.tipo_operacion,
                     modalidad = EXCLUDED.modalidad,
                     incoterm = EXCLUDED.incoterm,
                     proveedor = EXCLUDED.proveedor,
                     cliente = EXCLUDED.cliente,
                     nit_cliente = EXCLUDED.nit_cliente,
                     origen = EXCLUDED.origen,
                     destino = EXCLUDED.destino,
                     descripcion_mercancia = EXCLUDED.descripcion_mercancia,
                     estado = EXCLUDED.estado,
                     tipo_carga = EXCLUDED.tipo_carga,
                     tipo_contenedor = EXCLUDED.tipo_contenedor,
                     cantidad_contenedores = EXCLUDED.cantidad_contenedores,
                     numero_contenedor = EXCLUDED.numero_contenedor,
                     cantidad_bultos = EXCLUDED.cantidad_bultos,
                     peso_kg = EXCLUDED.peso_kg,
                     volumen_m3 = EXCLUDED.volumen_m3,
                     transportista = EXCLUDED.transportista,
                     tipo_documento = EXCLUDED.tipo_documento,
                     nombre_documento = EXCLUDED.nombre_documento,
                     fecha_bodega_origen = EXCLUDED.fecha_bodega_origen,
                     fecha_etd = EXCLUDED.fecha_etd,
                     fecha_atd = EXCLUDED.fecha_atd,
                     fecha_eta = EXCLUDED.fecha_eta,
                     fecha_ata = EXCLUDED.fecha_ata,
                     fecha_bodega_destino = EXCLUDED.fecha_bodega_destino,
                     fecha_nacionalizacion = EXCLUDED.fecha_nacionalizacion,
                     fecha_despacho_destino = EXCLUDED.fecha_despacho_destino,
                     fecha_planilla = EXCLUDED.fecha_planilla,
                     fecha_entrega_contenedor = EXCLUDED.fecha_entrega_contenedor,
                     fecha_devolucion_real_contenedor = EXCLUDED.fecha_devolucion_real_contenedor,
                     dias_libres = EXCLUDED.dias_libres,
                     dias_restantes_entrega = EXCLUDED.dias_restantes_entrega,
                     dias_demora_contenedor = EXCLUDED.dias_demora_contenedor,
                     valor_dia_demora = EXCLUDED.valor_dia_demora,
                     valor_total_demora = EXCLUDED.valor_total_demora,
                     deposito_contenedor = EXCLUDED.deposito_contenedor,
                     fecha_solicitud_anticipo = EXCLUDED.fecha_solicitud_anticipo,
                     fecha_pago_anticipo = EXCLUDED.fecha_pago_anticipo,
                     valor_anticipo = EXCLUDED.valor_anticipo,
                     factura_proveedor = EXCLUDED.factura_proveedor,
                     factura_tcc = EXCLUDED.factura_tcc,
                     numero_factura = EXCLUDED.numero_factura,
                     fecha_factura = EXCLUDED.fecha_factura,
                     descripcion_gasto = EXCLUDED.descripcion_gasto,
                     valor_gasto_usd = EXCLUDED.valor_gasto_usd,
                     subtotal_factura_usd = EXCLUDED.subtotal_factura_usd,
                     iva_usd = EXCLUDED.iva_usd,
                     total_factura_usd = EXCLUDED.total_factura_usd,
                     comentario = EXCLUDED.comentario,
                     fecha_comentario = EXCLUDED.fecha_comentario;";

            var command = new CommandDefinition(
                query,
                rowsList,
                transaction: _session.Transaction,
                cancellationToken: cancellationToken);

            // Dapper ejecuta la sentencia una vez por cada elemento de rowsList (batch) y devuelve
            // la suma de filas afectadas.
            return await _session.Connection.ExecuteAsync(command);
        }

        public async Task<IReadOnlyDictionary<String, ApplicationDataSheetChangeSnapshot>> GetChangeSnapshotsAsync(
            IEnumerable<String> documentNumbers, CancellationToken cancellationToken = default)
        {
            var documentsList = documentNumbers?.Where(d => !String.IsNullOrWhiteSpace(d)).Distinct().ToList()
                ?? new List<String>();

            if (documentsList.Count == 0)
                return new Dictionary<String, ApplicationDataSheetChangeSnapshot>(StringComparer.Ordinal);

            await _session.EnsureConnectionOpenAsync(cancellationToken);

            // Solo se seleccionan las columnas necesarias para detectar cambios (no la fila
            // completa de ~50 columnas), acotado a los documentos de la ronda actual (= ANY), en un
            // único round-trip: minimiza el impacto en rendimiento/transferencia de datos de este
            // paso adicional. AS con el nombre exacto de la propiedad porque Connection360.Etl.App no
            // habilita Dapper.DefaultTypeMap.MatchNamesWithUnderscores (eso solo se configura en el
            // proceso de la API principal, Connection360.Api/Program.cs).
            // fecha_comentario es DATE en Postgres: Npgsql 10 lo devuelve por defecto como
            // System.DateOnly, que Dapper no puede convertir automáticamente a una propiedad
            // DateTime (ApplicationDataSheetChangeSnapshot.FechaComentario). El cast ::timestamp
            // fuerza a Postgres a devolver un timestamp, que Npgsql sí mapea a DateTime.
            const string query = @"
                SELECT id AS ""Id"",
                       documento_transporte_hbl AS ""DocumentoTransporteHbl"",
                       estado AS ""Estado"",
                       comentario AS ""Comentario"",
                       fecha_comentario::timestamp AS ""FechaComentario""
                FROM connection360write.application_data_sheet
                WHERE documento_transporte_hbl = ANY(@Documents);";

            var command = new CommandDefinition(
                query,
                new { Documents = documentsList },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken);

            var rows = await _session.Connection.QueryAsync<ApplicationDataSheetChangeSnapshot>(command);

            return rows.ToDictionary(r => r.DocumentoTransporteHbl, StringComparer.Ordinal);
        }

        public async Task<IReadOnlyDictionary<String, Int64>> GetIdsByDocumentAsync(
            IEnumerable<String> documentNumbers, CancellationToken cancellationToken = default)
        {
            var documentsList = documentNumbers?.Where(d => !String.IsNullOrWhiteSpace(d)).Distinct().ToList()
                ?? new List<String>();

            if (documentsList.Count == 0)
                return new Dictionary<String, Int64>(StringComparer.Ordinal);

            await _session.EnsureConnectionOpenAsync(cancellationToken);

            // Solo Id + documento_transporte_hbl (ni siquiera las 4 columnas de
            // GetChangeSnapshotsAsync): esta consulta solo se usa para resolver el Id recién
            // generado de los documentos NUEVOS de la ronda, después del upsert.
            const string query = @"
                SELECT id AS ""Id"",
                       documento_transporte_hbl AS ""DocumentoTransporteHbl""
                FROM connection360write.application_data_sheet
                WHERE documento_transporte_hbl = ANY(@Documents);";

            var command = new CommandDefinition(
                query,
                new { Documents = documentsList },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken);

            var rows = await _session.Connection.QueryAsync<DocumentIdProjection>(command);

            return rows.ToDictionary(r => r.DocumentoTransporteHbl, r => r.Id, StringComparer.Ordinal);
        }

        // Proyección interna únicamente para el mapeo de Dapper de GetIdsByDocumentAsync: no cruza
        // el puerto de dominio (que expone un simple IReadOnlyDictionary<String, Int64>), así que no
        // hace falta declararla en Connection360.Etl.Domain.
        private sealed class DocumentIdProjection
        {
            public Int64 Id { get; set; }
            public String DocumentoTransporteHbl { get; set; } = String.Empty;
        }
    }
}
