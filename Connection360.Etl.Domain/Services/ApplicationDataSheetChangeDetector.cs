using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Domain.Interfaces;
using System;
using System.Collections.Generic;

namespace Connection360.Etl.Domain.Services
{
    /// <summary>
    /// Implementación de <see cref="IApplicationDataSheetChangeDetector"/>: para cada documento de
    /// esta ronda, compara Estado/Comentario/FechaComentario contra el snapshot "antes" -si el
    /// documento no existía previamente, se compara contra un snapshot vacío en vez de omitirlo, para
    /// que su aparición también cuente como cambio (ver <see cref="ApplicationDataSheetChange.IsNewDocument"/>)-
    /// y arma un <see cref="ApplicationDataSheetChange"/> únicamente cuando hay un cambio real. Solo
    /// hace búsquedas en un diccionario ya cargado en memoria (O(1) por documento): no ejecuta
    /// consultas ni operaciones costosas, para minimizar el impacto en rendimiento de cada ronda.
    /// </summary>
    public class ApplicationDataSheetChangeDetector : IApplicationDataSheetChangeDetector
    {
        // Snapshot "vacío" usado como valor "antes" para un documento que no existía todavía en
        // application_data_sheet: Estado/Comentario en blanco y FechaComentario en su valor por
        // defecto (equivalente a DateParsingExtensions.ToDateTimeOrMin() para un valor ausente), para
        // que la MISMA lógica de comparación de abajo aplique sin distinguir casos.
        private static readonly ApplicationDataSheetChangeSnapshot EmptySnapshot = new();

        public IReadOnlyList<ApplicationDataSheetChange> DetectChanges(
            IReadOnlyList<ApplicationDataSheet> currentSheets,
            IReadOnlyDictionary<String, ApplicationDataSheetChangeSnapshot> previousSnapshotsByDocument)
        {
            if (currentSheets is null)
                throw new ArgumentNullException(nameof(currentSheets));
            if (previousSnapshotsByDocument is null)
                throw new ArgumentNullException(nameof(previousSnapshotsByDocument));

            var changes = new List<ApplicationDataSheetChange>();

            foreach (var sheet in currentSheets)
            {
                Boolean isNewDocument = !previousSnapshotsByDocument.TryGetValue(sheet.DocumentoTransporteHbl, out var previous);
                if (isNewDocument)
                    previous = EmptySnapshot;

                Boolean stateChanged = !String.Equals(previous.Estado, sheet.Estado, StringComparison.Ordinal);
                Boolean commentChanged = !String.Equals(previous.Comentario, sheet.Comentario, StringComparison.Ordinal)
                    || previous.FechaComentario != sheet.FechaComentario;

                // Ni siquiera un documento nuevo genera un cambio si viene completamente vacío
                // (Estado/Comentario/FechaComentario en blanco): no hay nada que notificar.
                if (!stateChanged && !commentChanged)
                    continue;

                changes.Add(new ApplicationDataSheetChange
                {
                    DocumentoTransporteHbl = sheet.DocumentoTransporteHbl,
                    NitCliente = sheet.NitCliente,
                    // Para un documento nuevo, previous.Id es 0 (EmptySnapshot): el Id real recién se
                    // genera en el upsert de esta ronda y lo completa el llamador (RunEtlProcessUseCase)
                    // después, vía IApplicationDataSheetRepository.GetIdsByDocumentAsync.
                    IdOperacion = previous.Id,
                    IsNewDocument = isNewDocument,
                    StateChanged = stateChanged,
                    EstadoAnterior = previous.Estado,
                    NuevoEstado = sheet.Estado,
                    CommentChanged = commentChanged,
                });
            }

            return changes;
        }
    }
}
