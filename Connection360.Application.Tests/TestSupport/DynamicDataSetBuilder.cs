using Connection360.Domain.Entities;

namespace Connection360.Application.Tests.TestSupport
{
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

        public static DynamicDataSet Empty() => new(Enumerable.Empty<String>(), Enumerable.Empty<DynamicRecord>());
    }
}
