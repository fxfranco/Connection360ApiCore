using Connection360.Etl.Domain.Constants;
using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Connection360.Etl.Domain.Services
{
    /// <summary>
    /// Implementación del paso "Transform". La lógica de lectura de cada campo reutiliza el mismo
    /// criterio ya usado en Connection360.Domain.Services.MyShipmentsDomainService.GetDetailsShipments
    /// (acceso por <see cref="ExternalDataFields"/> sobre el <see cref="DynamicRecord"/> resultante
    /// del merge), extendido aquí para convertir cada valor de texto al tipo de columna real de
    /// connection360write.application_data_sheet (fechas, enteros y montos), ya que allí se dejaban
    /// como String porque la respuesta de la API solo se mostraba, no se persistía.
    /// </summary>
    public class ShipmentsDataSheetMappingService : IShipmentsDataSheetMappingService
    {
        public IReadOnlyList<ApplicationDataSheet> Map(DynamicDataSet unifiedDataSet)
        {
            if (unifiedDataSet is null)
                throw new ArgumentNullException(nameof(unifiedDataSet));

            // Índice del último cambio de estado por documento (histórico DATALOGS), usando el
            // mismo criterio de orden (ID_LOG ascendente) que DetailsHistoryShipmentsDomainService.
            //var latestLogByDocument = BuildLatestLogIndex(historyDataSet);

            var result = new List<ApplicationDataSheet>(unifiedDataSet.Rows.Count);

            foreach (var record in unifiedDataSet.Rows)
            {
                var documentNumber = record[ExternalDataFields.DocumentNumber];

                // Sin número de documento no hay llave de negocio para el upsert: se descarta la fila.
                if (String.IsNullOrWhiteSpace(documentNumber))
                    continue;

                var sheet = new ApplicationDataSheet
                {
                    FechaCreacion = record[ExternalDataFields.CreationDate].ToDateTimeOrMin(),
                    TipoOperacion = record[ExternalDataFields.OperationType],
                    Modalidad = record[ExternalDataFields.ShipmentMode],
                    Incoterm = record[ExternalDataFields.Incoterm],
                    Proveedor = record[ExternalDataFields.Supplier],
                    Cliente = record[ExternalDataFields.ClientName],
                    NitCliente = record[ExternalDataFields.ClientNit],
                    Origen = record[ExternalDataFields.Origin],
                    Destino = record[ExternalDataFields.Destination],
                    DescripcionMercancia = record[ExternalDataFields.MerchandiseDescription],
                    Estado = record[ExternalDataFields.State],

                    TipoCarga = record[ExternalDataFields.LoadType],
                    TipoContenedor = NullIfEmpty(record[ExternalDataFields.ContainerType]),
                    CantidadContenedores = record[ExternalDataFields.ContainerAmount].ToInt32OrDefault(),
                    NumeroContenedor = NullIfEmpty(record[ExternalDataFields.ContainerNumber]),
                    CantidadBultos = record[ExternalDataFields.PackagesNumbers].ToInt32OrDefault(),
                    PesoKg = record[ExternalDataFields.WeightKg].ToDecimalOrDefault(),
                    VolumenM3 = record[ExternalDataFields.VolumeM3].ToDecimalOrDefault(),

                    Transportista = record[ExternalDataFields.Carrier],
                    TipoDocumento = record[ExternalDataFields.DocumentType],
                    NombreDocumento = record[ExternalDataFields.DocumentType],
                    DocumentoTransporteHbl = documentNumber,

                    FechaBodegaOrigen = record[ExternalDataFields.StoreOriginDate].ToDateTimeOrMin(),
                    FechaEtd = record[ExternalDataFields.ETDDate].ToDateTimeOrMin(),
                    FechaAtd = record[ExternalDataFields.ATDDate].ToDateTimeOrMin(),
                    FechaEta = record[ExternalDataFields.ETADate].ToDateTimeOrMin(),
                    FechaAta = record[ExternalDataFields.ATADate].ToDateTimeOrMin(),
                    FechaBodegaDestino = record[ExternalDataFields.StoreDestinationDate].ToDateTimeOrMin(),
                    FechaNacionalizacion = record[ExternalDataFields.NationalizationDate].ToDateTimeOrMin(),
                    FechaDespachoDestino = record[ExternalDataFields.DispatchDestinationDate].ToDateTimeOrMin(),
                    FechaPlanilla = record[ExternalDataFields.FormDate].ToDateTimeOrMin(),
                    FechaEntregaContenedor = record[ExternalDataFields.ContainerDeliveryDate].ToDateTimeOrMin(),
                    FechaDevolucionRealContenedor = record[ExternalDataFields.ActualContainerReturnDate].ToNullableDate(),

                    DiasLibres = record[ExternalDataFields.DaysOff].ToInt32OrDefault(),
                    DiasRestantesEntrega = record[ExternalDataFields.DaysRemainingDelivery].ToInt32OrDefault(),
                    DiasDemoraContenedor = record[ExternalDataFields.ContainerDelayDays].ToInt32OrDefault(),
                    ValorDiaDemora = record[ExternalDataFields.CostDayOfDelay].ToDecimalOrDefault(),
                    ValorTotalDemora = record[ExternalDataFields.TotalCostContainerDelays].ToDecimalOrDefault(),
                    DepositoContenedor = record[ExternalDataFields.ContainerDepot].ToDecimalOrDefault(),

                    FechaSolicitudAnticipo = record[ExternalDataFields.AdvancePaymentRequestDate].ToDateTimeOrMin(),
                    FechaPagoAnticipo = record[ExternalDataFields.AdvancePaymentDate].ToDateTimeOrMin(),
                    ValorAnticipo = record[ExternalDataFields.AdvancePaymentAmount].ToDecimalOrDefault(),
                    FacturaProveedor = record[ExternalDataFields.SupplierInvoice],
                    FacturaTcc = record[ExternalDataFields.TCCInvoice],
                    NumeroFactura = record[ExternalDataFields.InvoiceNumber],
                    FechaFactura = record[ExternalDataFields.InvoiceDate].ToDateTimeOrMin(),
                    DescripcionGasto = record[ExternalDataFields.ExpenseDescription],
                    ValorGastoUsd = record[ExternalDataFields.ExpenseAmountUSD].ToDecimalOrDefault(),
                    SubtotalFacturaUsd = record[ExternalDataFields.InvoiceSubtotalUSD].ToDecimalOrDefault(),
                    IvaUsd = record[ExternalDataFields.IvaUSD].ToDecimalOrDefault(),
                    TotalFacturaUsd = record[ExternalDataFields.TotalInvoiceUSD].ToDecimalOrDefault(),

                    //Comentarios que después debe validar si se registra en otra tabla.
                    Comentario = record[ExternalDataFields.Comment],
                    FechaComentario = record[ExternalDataFields.CommentDate].ToDateTimeOrMin(),
                };

                //if (latestLogByDocument.TryGetValue(documentNumber, out var latestLog))
                //{
                //    sheet.Comentario = latestLog[ExternalDataFields.MessageLog];
                //    sheet.FechaComentario = latestLog[ExternalDataFields.ChangeDateLog].ToDateTimeOrMin();
                //}

                result.Add(sheet);
            }

            return result;
        }

        /// <summary>
        /// Construye, para cada documento de transporte, el registro de log más reciente (mayor
        /// ID_LOG) del histórico DATALOGS. Mismo criterio de orden que
        /// Connection360.Domain.Services.DetailsHistoryShipmentsDomainService.GetDetailsHistoryShipments,
        /// pero calculado una sola vez para todos los documentos en lugar de por documento individual.
        /// </summary>
        //private static Dictionary<String, DynamicRecord> BuildLatestLogIndex(DynamicDataSet historyDataSet)
        //{
        //    var index = new Dictionary<String, DynamicRecord>(StringComparer.OrdinalIgnoreCase);

        //    if (historyDataSet is null)
        //        return index;

        //    var groupedByDocument = historyDataSet.Rows
        //        .Where(r => !String.IsNullOrWhiteSpace(r[ExternalDataFields.DocumentNumber]))
        //        .GroupBy(r => r[ExternalDataFields.DocumentNumber], StringComparer.OrdinalIgnoreCase);

        //    foreach (var group in groupedByDocument)
        //    {
        //        var latest = group
        //            .OrderBy(r => Int64.TryParse(r[ExternalDataFields.IdLog], out Int64 id) ? id : 0)
        //            .Last();

        //        index[group.Key] = latest;
        //    }

        //    return index;
        //}

        private static String? NullIfEmpty(String value) => String.IsNullOrWhiteSpace(value) ? null : value;
    }
}
