using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Domain.Interfaces;
using System;
using System.Collections.Generic;

namespace Connection360.Etl.Domain.Services
{
    /// <summary>
    /// Implementación de <see cref="IApplicationDataSheetChangeDetector"/>: para cada documento de
    /// esta ronda, compara Estado/Comentario/FechaComentario -y, desde el punto de vista de "otra
    /// información", los 20 campos de <see cref="ApplicationDataSheetChange.OtherInfoChanged"/>- contra el snapshot "antes" -si el
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
            ArgumentNullException.ThrowIfNull(currentSheets);
            ArgumentNullException.ThrowIfNull(previousSnapshotsByDocument);

            var changes = new List<ApplicationDataSheetChange>();

            foreach (var sheet in currentSheets)
            {
                Boolean isNewDocument = !previousSnapshotsByDocument.TryGetValue(sheet.DocumentoTransporteHbl, out var previous);
                if (isNewDocument)
                    previous = EmptySnapshot;

                Boolean stateChanged = !String.Equals(previous!.Estado, sheet.Estado, StringComparison.Ordinal);
                Boolean commentChanged = !String.Equals(previous.Comentario, sheet.Comentario, StringComparison.Ordinal)
                    || previous.FechaComentario != sheet.FechaComentario;

                // "Otra información": solo para documentos que YA existían. Un documento nuevo pasa de
                // "nada" a todos sus valores y su aparición ya se notifica por estado/comentario; marcarlo
                // también como cambio de información general generaría un tercer mensaje redundante.
                Boolean otherInfoChanged = !isNewDocument && HasOtherInfoChanged(previous, sheet);

                // Ni siquiera un documento nuevo genera un cambio si viene completamente vacío
                // (Estado/Comentario/FechaComentario en blanco): no hay nada que notificar.
                if (!stateChanged && !commentChanged && !otherInfoChanged)
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
                    OtherInfoChanged = otherInfoChanged,
                    Origen = sheet.Origen,
                    Destino = sheet.Destino,
                    // Si el comentario cambió son los NUEVOS (los que trajo la API); si no cambió, son los
                    // que ya estaban, que por definición coinciden con los actuales.
                    Comentario = sheet.Comentario,
                    FechaComentario = sheet.FechaComentario,
                    ChangeUser = sheet.ChangeUser,
                });
            }

            return changes;
        }

        /// <summary>
        /// true si cambió alguno de los campos de "otra información". Reglas de comparación (para no
        /// generar cambios falsos en cada corrida): las fechas se comparan por día (la columna es DATE);
        /// los importes con 2 decimales y redondeo "away from zero" (la columna es NUMERIC(12,2) y Postgres
        /// redondea igual); el texto de forma exacta.
        /// </summary>
        private static Boolean HasOtherInfoChanged(ApplicationDataSheetChangeSnapshot previous, ApplicationDataSheet current)
        {
            return !String.Equals(previous.Transportista ?? String.Empty, current.Transportista ?? String.Empty, StringComparison.Ordinal)
                || DiffersByDay(previous.FechaSolicitudAnticipo, current.FechaSolicitudAnticipo)
                || DiffersByAmount(previous.ValorAnticipo, current.ValorAnticipo)
                || DiffersByDay(previous.FechaPagoAnticipo, current.FechaPagoAnticipo)
                || DiffersByDay(previous.FechaBodegaOrigen, current.FechaBodegaOrigen)
                || DiffersByDay(previous.FechaEtd, current.FechaEtd)
                || DiffersByDay(previous.FechaAtd, current.FechaAtd)
                || DiffersByDay(previous.FechaEta, current.FechaEta)
                || DiffersByDay(previous.FechaAta, current.FechaAta)
                || DiffersByDay(previous.FechaBodegaDestino, current.FechaBodegaDestino)
                || DiffersByDay(previous.FechaNacionalizacion, current.FechaNacionalizacion)
                || DiffersByDay(previous.FechaDespachoDestino, current.FechaDespachoDestino)
                || DiffersByDay(previous.FechaPlanilla, current.FechaPlanilla)
                || DiffersByDay(previous.FechaEntregaContenedor, current.FechaEntregaContenedor)
                || previous.DiasLibres != current.DiasLibres
                || previous.DiasRestantesEntrega != current.DiasRestantesEntrega
                || DiffersByDay(previous.FechaDevolucionRealContenedor, current.FechaDevolucionRealContenedor)
                || previous.DiasDemoraContenedor != current.DiasDemoraContenedor
                || DiffersByAmount(previous.ValorDiaDemora, current.ValorDiaDemora)
                || DiffersByAmount(previous.ValorTotalDemora, current.ValorTotalDemora);
        }

        private static Boolean DiffersByDay(DateTime previous, DateTime current) => previous.Date != current.Date;

        // Una fecha ausente (null) y una fecha presente son distintas; dos ausentes son iguales.
        private static Boolean DiffersByDay(DateTime? previous, DateTime? current) => previous?.Date != current?.Date;

        private static Boolean DiffersByAmount(Decimal previous, Decimal current) =>
            Math.Round(previous, 2, MidpointRounding.AwayFromZero) != Math.Round(current, 2, MidpointRounding.AwayFromZero);
    }
}
