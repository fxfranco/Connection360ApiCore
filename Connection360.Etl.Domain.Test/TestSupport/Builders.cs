using Connection360.Etl.Domain.Entities;

namespace Connection360.Etl.Domain.Test.TestSupport
{
    internal static class Builders
    {
        public static DynamicRecord Record(params (String Key, String Value)[] fields)
        {
            var dict = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, value) in fields)
                dict[key] = value;
            return new DynamicRecord(dict);
        }

        public static DynamicDataSet DataSet(IEnumerable<String> fields, params DynamicRecord[] rows)
            => new(fields, rows);
    }
}
