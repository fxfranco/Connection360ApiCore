using Connection360.Etl.Domain.Constants;
using Connection360.Etl.Domain.Entities;

namespace Connection360.Etl.Application.Tests.Support
{
    /// <summary>Utilidades para construir secuencias asíncronas y datasets de prueba.</summary>
    internal static class AsyncSequence
    {
        public static async IAsyncEnumerable<T> From<T>(params T[] items)
        {
            foreach (T item in items)
            {
                await Task.CompletedTask;
                yield return item;
            }
        }

        public static async IAsyncEnumerable<T> Throwing<T>(Exception exception, params T[] itemsBeforeFailure)
        {
            foreach (T item in itemsBeforeFailure)
            {
                await Task.CompletedTask;
                yield return item;
            }

            await Task.CompletedTask;
            throw exception;
        }
    }

    internal static class DataSets
    {
        public static DynamicDataSet WithDocuments(params String[] documents)
        {
            var rows = documents.Select(d => new DynamicRecord(new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase)
            {
                [ExternalDataFields.DocumentNumber] = d
            }));

            return new DynamicDataSet(new[] { ExternalDataFields.DocumentNumber }, rows);
        }
    }
}
