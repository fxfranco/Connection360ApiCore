using Connection360.Etl.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Connection360.Etl.Application.UseCases.Paging
{
    /// <summary>
    /// Sincroniza varias fuentes paginadas (una por API externa, cada una un
    /// <see cref="IAsyncEnumerable{T}"/> de <see cref="DynamicDataSet"/> devuelto por
    /// <c>IExternalDataGateway.FetchDataPagedAsync</c>) en "rondas": en la ronda N se obtiene la
    /// página N de cada fuente que todavía tenga datos. Una fuente que ya se agotó contribuye con un
    /// <see cref="DynamicDataSet.Empty"/> al resto de las rondas en vez de detener a las demás, y la
    /// iteración completa termina cuando TODAS las fuentes se agotaron.
    /// <para>
    /// Esto es lo que le permite a RunEtlProcessUseCase combinar (merge) y cargar página por página
    /// en transacciones independientes SIN necesidad de saber si la paginación está habilitada o no:
    /// con paginación deshabilitada, cada fuente entrega una sola "página" (el 100% de sus datos) y
    /// la iteración termina después de la primera ronda, reproduciendo el comportamiento actual.
    /// </para>
    /// </summary>
    public sealed class MergedPageEnumerator : IAsyncDisposable
    {
        private sealed class SourceCursor
        {
            public required String ApiName { get; init; }
            public required IAsyncEnumerator<DynamicDataSet> Enumerator { get; init; }
            public Boolean Exhausted { get; set; }
        }

        private readonly List<SourceCursor> _cursors;

        public MergedPageEnumerator(IEnumerable<(String ApiName, IAsyncEnumerable<DynamicDataSet> Pages)> sources, CancellationToken cancellationToken)
        {
            _cursors = sources
                .Select(source => new SourceCursor
                {
                    ApiName = source.ApiName,
                    Enumerator = source.Pages.GetAsyncEnumerator(cancellationToken)
                })
                .ToList();
        }

        /// <summary>
        /// Avanza una ronda pidiendo la siguiente página a cada fuente que no se haya agotado aún.
        /// </summary>
        /// <returns>
        /// <c>null</c> cuando todas las fuentes ya se agotaron (fin de la iteración); en caso
        /// contrario, la página de esa ronda por cada API (<see cref="DynamicDataSet.Empty"/> para
        /// las fuentes ya agotadas).
        /// </returns>
        public async Task<IReadOnlyDictionary<String, DynamicDataSet>?> MoveNextRoundAsync()
        {
            if (_cursors.All(cursor => cursor.Exhausted))
                return null;

            var pageByApi = new Dictionary<String, DynamicDataSet>(StringComparer.OrdinalIgnoreCase);

            foreach (var cursor in _cursors)
            {
                if (cursor.Exhausted)
                {
                    pageByApi[cursor.ApiName] = DynamicDataSet.Empty;
                    continue;
                }

                Boolean hasPage = await cursor.Enumerator.MoveNextAsync();
                if (!hasPage)
                {
                    cursor.Exhausted = true;
                    pageByApi[cursor.ApiName] = DynamicDataSet.Empty;
                    continue;
                }

                pageByApi[cursor.ApiName] = cursor.Enumerator.Current;
            }

            return pageByApi;
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var cursor in _cursors)
            {
                await cursor.Enumerator.DisposeAsync();
            }
        }
    }
}
