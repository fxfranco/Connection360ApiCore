using Connection360.Etl.Domain.Entities;
using System;
using System.Collections.Generic;

namespace Connection360.Etl.Domain.Interfaces
{
    /// <summary>
    /// Puerto de dominio: detecta, por documento de transporte (HBL), si hubo cambios en los campos
    /// ESTADO, COMENTARIO y/o FECHA COMENTARIO entre lo que ya existe en connection360write.
    /// application_data_sheet ("antes") y lo recién transformado desde las APIs externas
    /// ("después"). Un documento que NO existía previamente en la tabla se trata igual que un cambio
    /// (pasa de "nada" a su valor actual): ver <see cref="ApplicationDataSheetChange.IsNewDocument"/>.
    /// Esto NO aplica durante la migración inicial (ver RunEtlProcessUseCase): en esa corrida no se
    /// llama a este detector en absoluto, ya que absolutamente todos los documentos son "nuevos" y
    /// compararlos generaría un cambio (y su notificación) por cada uno.
    /// <para>
    /// Es una comparación puramente en memoria (sin I/O): quien resuelve el "antes" (una consulta
    /// acotada a los documentos de la ronda actual, que también trae el "Id" BIGSERIAL de cada
    /// documento) es el llamador, vía
    /// <see cref="Ports.Persistence.IApplicationDataSheetRepository.GetChangeSnapshotsAsync"/>, para
    /// que este servicio se pueda probar unitariamente sin base de datos y se mantenga agnóstico del
    /// motor de persistencia (extensible a futuro sin cambios aquí).
    /// </para>
    /// </summary>
    public interface IApplicationDataSheetChangeDetector
    {
        /// <param name="currentSheets">Filas recién transformadas en esta ronda (después).</param>
        /// <param name="previousSnapshotsByDocument">
        /// Id/Estado/Comentario/FechaComentario tal como están HOY en la tabla principal (antes),
        /// acotado a los documentos de esta ronda. Un documento ausente aquí es un documento NUEVO:
        /// se compara contra un snapshot vacío (Estado/Comentario en blanco), por lo que normalmente
        /// sí genera un cambio (<see cref="ApplicationDataSheetChange.IsNewDocument"/> true) - salvo
        /// que también venga con Estado/Comentario/FechaComentario vacíos, caso en el que no hay nada
        /// que notificar. El "Id" del snapshot (BIGSERIAL de application_data_sheet, no afectado por
        /// el upsert) es el que se usa como ApplicationDataSheetChange.IdOperacion para un documento
        /// EXISTENTE; para uno NUEVO ese Id todavía no existe al momento de esta comparación (se
        /// resuelve después del upsert, fuera de este servicio).
        /// </param>
        /// <returns>Solo los documentos en los que se detectó un cambio de estado y/o de comentario.</returns>
        IReadOnlyList<ApplicationDataSheetChange> DetectChanges(
            IReadOnlyList<ApplicationDataSheet> currentSheets,
            IReadOnlyDictionary<String, ApplicationDataSheetChangeSnapshot> previousSnapshotsByDocument);
    }
}
