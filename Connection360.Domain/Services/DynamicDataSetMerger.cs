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

            List<String> mergedFields = MergeFields(sets);
            HashSet<String> validKeys = ResolveValidKeys(sets, joinField, joinType);
            List<DynamicRecord> mergedRows = MergeRows(sets, joinField, validKeys);

            return new DynamicDataSet(mergedFields, mergedRows);
        }

        /// <summary>Unión de columnas sin duplicados, preservando el orden de aparición.</summary>
        private static List<String> MergeFields(List<DynamicDataSet> sets)
        {
            var mergedFields = new List<String>();
            foreach (var set in sets)
                foreach (var field in set.AvailableFields.Where(candidate => !mergedFields.Contains(candidate, StringComparer.OrdinalIgnoreCase)))
                    mergedFields.Add(field);

            return mergedFields;
        }

        /// <summary>Llaves presentes en cada dataset (para poder resolver Inner o FullOuter).</summary>
        private static HashSet<String> ResolveValidKeys(List<DynamicDataSet> sets, String joinField, DataSetJoinType joinType)
        {
            var keysPerSet = sets
                .Select(set => set.Rows
                    .Select(r => r.GetValue(joinField))
                    .Where(k => !String.IsNullOrEmpty(k))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase))
                .ToList();

            return joinType == DataSetJoinType.Inner
                ? keysPerSet.Aggregate((a, b) => a.Intersect(b, StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase))
                : keysPerSet.SelectMany(k => k).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Fusión de filas por llave, evitando pisar valores ya presentes con vacíos.</summary>
        private static List<DynamicRecord> MergeRows(List<DynamicDataSet> sets, String joinField, HashSet<String> validKeys)
        {
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
                        if (!merged.TryGetValue(field, out String? existingValue) || String.IsNullOrEmpty(existingValue))
                            merged[field] = value;
                    }
                }
            }

            return keyOrder.Select(k => new DynamicRecord(accumulator[k])).ToList();
        }
    }
}
