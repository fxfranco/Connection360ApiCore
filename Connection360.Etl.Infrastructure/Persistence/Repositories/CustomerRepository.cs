using Connection360.Etl.Domain.Ports.Persistence;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Connection360.Etl.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Adaptador de <see cref="ICustomerRepository"/>: inserta en PostgreSQL, tabla
    /// connection360write.customers (ver Documents/scriptcustomers.sql). Mismo patrón Dapper +
    /// <see cref="DbSession"/> que el resto de repositorios de la ETL (participa en la transacción
    /// activa de la ronda).
    /// </summary>
    public class CustomerRepository : ICustomerRepository
    {
        private readonly DbSession _session;

        public CustomerRepository(DbSession session)
        {
            _session = session;
        }

        public async Task<Int32> InsertMissingAsync(IEnumerable<String> identifications, CancellationToken cancellationToken = default)
        {
            var identificationsList = identifications?
                .Where(i => !String.IsNullOrWhiteSpace(i))
                .Distinct(StringComparer.Ordinal)
                .ToList()
                ?? new List<String>();

            if (identificationsList.Count == 0)
                return 0;

            await _session.EnsureConnectionOpenAsync(cancellationToken);

            // Una sola sentencia por lote: unnest(@Identifications) expande el arreglo y el
            // NOT EXISTS descarta las identificaciones que ya están en la tabla, así que no se
            // insertan duplicados ni hay un SELECT + INSERT por cliente (sin N+1). La tabla no
            // define UNIQUE sobre "identificacion": la ETL corre en una sola instancia y el
            // lote va en la transacción de la ronda; un índice UNIQUE en esa columna (recomendado)
            // además garantizaría la no duplicidad ante escrituras concurrentes y aceleraría este
            // NOT EXISTS y la consulta de canales.
            const string query = @"
                INSERT INTO connection360write.customers (identificacion)
                SELECT t.identificacion
                FROM unnest(@Identifications) AS t(identificacion)
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM connection360write.customers c
                    WHERE c.identificacion = t.identificacion);";

            var command = new CommandDefinition(
                query,
                new { Identifications = identificationsList },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken);

            return await _session.Connection.ExecuteAsync(command);
        }
    }
}
