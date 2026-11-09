using System;
using System.Collections.Generic;

namespace Connection360.Etl.Domain.Entities
{
    /// <summary>
    /// Fila genérica de datos provenientes de una API externa, indexada por nombre de campo.
    /// Copiado de Connection360.Domain.Entities.DynamicRecord para que el proceso ETL (proyectos
    /// Connection360.Etl.*) no dependa en tiempo de ejecución del ensamblado de la API principal
    /// (Connection360.Domain/Application/Infrastructure), conservando exactamente la misma lógica.
    /// </summary>
    public class DynamicRecord
    {
        // Diccionario interno con los valores de la fila (Clave = Nombre Campo)
        private readonly Dictionary<String, String> _fields;

        public DynamicRecord(Dictionary<String, String> fields)
        {
            _fields = fields ?? new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
        }

        // Indexador indexado por nombre de campo
        public String this[String fieldName] => GetValue(fieldName);

        public String GetValue(String fieldName)
        {
            return _fields.TryGetValue(fieldName, out var value) ? value : String.Empty;
        }

        public bool HasField(String fieldName) => _fields.ContainsKey(fieldName);

        // Exposición de solo lectura para poder iterar los campos (ej: en mergers, mappers, etc.)
        public IReadOnlyDictionary<String, String> Fields => _fields;
    }
}
