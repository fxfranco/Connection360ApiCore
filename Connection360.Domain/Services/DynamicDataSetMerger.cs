using Connection360.Domain.Entities;
using Connection360.Domain.Enum;
using Connection360.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Connection360.Domain.Services
{
    public class DynamicDataSetMerger : IDynamicDataSetMerger
    {
        public DynamicDataSet Merge(IEnumerable<DynamicDataSet> dataSets, String joinField, DataSetJoinType joinType = DataSetJoinType.FullOuter)
        {
            var sets = dataSets?.Where(ds => ds is not null).ToList() ?? new List<DynamicDataSet>();

            if (sets.Count == 0)
                return new DynamicDataSet(Enumerable.Empty<String>(), Enumerable.Empty<DynamicRecord>());

            if (sets.Count == 1)
                return sets[0];

            if (String.IsNullOrWhiteSpace(joinField))
                throw new ArgumentException("Se debe indicar un campo de unión (joinField).", nameof(joinField));

            // 1. Unión de columnas sin duplicados, preservando el orden de aparición
            var mergedFields = new List<String>();
            foreach (var set in sets)
                foreach (var field in set.AvailableFields)
                    if (!mergedFields.Contains(field, StringComparer.OrdinalIgnoreCase))
                        mergedFields.Add(field);

            // 2. Llaves presentes en cada dataset (para poder resolver Inner o FullOuter)
            var keysPerSet = sets
                .Select(set => set.Rows
                    .Select(r => r.GetValue(joinField))
                    .Where(k => !String.IsNullOrEmpty(k))
                    .Select(k => k!)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase))
                .ToList();

            var validKeys = joinType == DataSetJoinType.Inner
                ? keysPerSet.Aggregate((a, b) => a.Intersect(b, StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase))
                : keysPerSet.SelectMany(k => k).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 3. Fusión de filas por llave, evitando pisar valores ya presentes con vacíos
            var accumulator = new Dictionary<String, Dictionary<String, String>>(StringComparer.OrdinalIgnoreCase);
            var keyOrder = new List<String>();

            foreach (var set in sets)
            {
                foreach (var record in set.Rows)
                {
                    var keyValue = record.GetValue(joinField);
                    if (String.IsNullOrEmpty(keyValue) || !validKeys.Contains(keyValue))
                        continue;

                    if (!accumulator.TryGetValue(keyValue, out var merged))
                    {
                        merged = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                        accumulator[keyValue] = merged;
                        keyOrder.Add(keyValue);
                    }

                    foreach (var (field, value) in record.Fields)
                    {
                        if (!merged.ContainsKey(field) || String.IsNullOrEmpty(merged[field]))
                            merged[field] = value;
                    }
                }
            }

            var mergedRows = keyOrder.Select(k => new DynamicRecord(accumulator[k])).ToList();
            return new DynamicDataSet(mergedFields, mergedRows);
        }
    }
}
