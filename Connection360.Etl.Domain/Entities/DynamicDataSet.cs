using System;
using System.Collections.Generic;
using System.Linq;

namespace Connection360.Etl.Domain.Entities
{
    /// <summary>
    /// Conjunto de filas (<see cref="DynamicRecord"/>) obtenidas de una API externa, junto con los
    /// nombres de campo disponibles. Copiado de Connection360.Domain.Entities.DynamicDataSet (ver
    /// nota en <see cref="DynamicRecord"/> sobre por qué se copia en vez de referenciar).
    /// </summary>
    public class DynamicDataSet
    {
        public IReadOnlyList<String> AvailableFields { get; }
        public IReadOnlyList<DynamicRecord> Rows { get; }

        public DynamicDataSet(IEnumerable<String> requestedFields, IEnumerable<DynamicRecord> rows)
        {
            AvailableFields = requestedFields.ToList().AsReadOnly();
            Rows = rows.ToList().AsReadOnly();
        }

        /// <summary>
        /// Instancia sin filas, reutilizada por el proceso ETL paginado para representar la
        /// "página" de una fuente que ya se agotó (para no detener la ronda de las demás fuentes).
        /// </summary>
        public static DynamicDataSet Empty { get; } = new DynamicDataSet(Enumerable.Empty<String>(), Enumerable.Empty<DynamicRecord>());
    }
}
