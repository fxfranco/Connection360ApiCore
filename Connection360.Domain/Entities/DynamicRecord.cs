using System;
using System.Collections.Generic;
using System.Text;

namespace Connection360.Domain.Entities
{
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
    }
}
