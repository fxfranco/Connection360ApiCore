using Connection360.Etl.Domain.Constants;
using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Domain.Interfaces;
using System;
using System.Collections.Generic;

namespace Connection360.Etl.Domain.Services
{
    /// <summary>
    /// Implementación del paso "Transform" del proceso ETL de logs. Reutiliza los mismos campos que
    /// ya consume Connection360.Domain.Services.DetailsHistoryShipmentsDomainService para el
    /// histórico de un envío puntual, pero aquí se recorre el dataset completo de una sola vez (sin
    /// filtrar por documento) para cargar TODOS los registros de log disponibles, sin combinarlos
    /// con ningún otro dataset (es un flujo 100% independiente del de la bodega de datos de envíos).
    /// </summary>
    public class LogStatusMappingService : ILogStatusMappingService
    {
        public IReadOnlyList<LogStatusTracking> Map(DynamicDataSet dataLogsDataSet)
        {
            if (dataLogsDataSet is null)
                throw new ArgumentNullException(nameof(dataLogsDataSet));

            var result = new List<LogStatusTracking>(dataLogsDataSet.Rows.Count);

            foreach (var record in dataLogsDataSet.Rows)
            {
                var documentNumber = record[ExternalDataFields.DocumentNumber];

                // Sin documento de transporte no hay operación a la que asociar el log: se descarta.
                if (String.IsNullOrWhiteSpace(documentNumber))
                    continue;

                result.Add(new LogStatusTracking
                {
                    IdOperacion = record[ExternalDataFields.IdLog].ToInt64OrDefault(),
                    DocumentoTransporteHbl = documentNumber,
                    FechaCambio = record[ExternalDataFields.ChangeDateLog].ToDateTimeOrMin(),
                    UsuarioCambio = record[ExternalDataFields.ChangeUserLog],
                    Mensaje = record[ExternalDataFields.MessageLog],
                    EstadoAnterior = record[ExternalDataFields.OldStateLog],
                    NuevoEstado = record[ExternalDataFields.NewStateLog]
                });
            }

            return result;
        }
    }
}
