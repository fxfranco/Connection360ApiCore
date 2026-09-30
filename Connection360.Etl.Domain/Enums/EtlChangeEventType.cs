using System;

namespace Connection360.Etl.Domain.Enums
{
    /// <summary>
    /// Tipo de evento que el proceso ETL principal encola en connection360write.outbox_messages
    /// cuando detecta un cambio relevante en un documento de transporte (ver
    /// Connection360.Etl.Domain.Interfaces.IApplicationDataSheetChangeDetector). Cada valor
    /// corresponde a un "event_type" propio (columna VARCHAR) y a la propiedad "Type" del JSON en
    /// la columna payload.
    /// </summary>
    public enum EtlChangeEventType
    {
        /// <summary>Cambio en el campo ESTADO del documento.</summary>
        ChangeState,

        /// <summary>Cambio en el campo COMENTARIO y/o FECHA COMENTARIO del documento.</summary>
        Comment
    }

    /// <summary>Conversión explícita entre <see cref="EtlChangeEventType"/> y el valor persistido en event_type/payload.EventType.</summary>
    public static class EtlChangeEventTypeExtensions
    {
        /// <summary>
        /// Valor exacto que se guarda en event_type y en la propiedad "Type" del JSON de payload. Se
        /// convierte manualmente (en vez de dejar que Dapper/System.Text.Json serialicen el enum)
        /// para no depender de un type handler ni de la representación por defecto del enum.
        /// </summary>
        public static String ToDbValue(this EtlChangeEventType eventType) => eventType switch
        {
            EtlChangeEventType.ChangeState => "ChangeState",
            EtlChangeEventType.Comment => "Comment",
            _ => throw new ArgumentOutOfRangeException(nameof(eventType), eventType, "EtlChangeEventType sin valor de base de datos asignado.")
        };
    }
}
