namespace Connection360.Domain.Entities
{
    public class DynamicDataSet
    {
        public IReadOnlyList<String> AvailableFields { get; }
        public IReadOnlyList<DynamicRecord> Rows { get; }

        public DynamicDataSet(IEnumerable<String> requestedFields, IEnumerable<DynamicRecord> rows)
        {
            AvailableFields = requestedFields.ToList().AsReadOnly();
            Rows = rows.ToList().AsReadOnly();
        }
    }
}
