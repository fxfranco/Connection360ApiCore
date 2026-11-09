using Connection360.Domain.Entities;

namespace Connection360.Domain.Tests.TestSupport
{
    /// <summary>
    /// Helper para construir DynamicDataSet / DynamicRecord de prueba de forma legible,
    /// evitando repetir la construcción de diccionarios en cada test de los servicios de dominio.
    /// </summary>
    internal static class DynamicDataSetBuilder
    {
        public static DynamicRecord Row(params (String Field, String Value)[] values)
        {
            var dict = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            foreach (var (field, value) in values)
            {
                dict[field] = value;
            }
            return new DynamicRecord(dict);
        }

        public static DynamicDataSet DataSet(IEnumerable<String> fields, params DynamicRecord[] rows)
        {
            return new DynamicDataSet(fields, rows);
        }
    }
}
