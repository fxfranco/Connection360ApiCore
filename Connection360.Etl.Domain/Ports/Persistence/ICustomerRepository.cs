using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Connection360.Etl.Domain.Ports.Persistence
{
    /// <summary>
    /// Puerto secundario (saliente): garantiza que cada cliente (NitCliente) procesado por la ETL
    /// exista en la tabla PostgreSQL connection360write.customers. Lo implementa
    /// Connection360.Etl.Infrastructure.
    /// </summary>
    public interface ICustomerRepository
    {
        /// <summary>
        /// Inserta en connection360write.customers (columna <c>identificacion</c>) únicamente las
        /// identificaciones que todavía NO existen; las que ya existen se omiten, y las repetidas
        /// dentro de <paramref name="identifications"/> o vacías se ignoran, de modo que nunca
        /// queda un cliente duplicado. Se hace en una sola sentencia por lote (un round-trip),
        /// para no penalizar el rendimiento de la carga.
        /// </summary>
        /// <returns>Cantidad de clientes nuevos insertados.</returns>
        Task<Int32> InsertMissingAsync(IEnumerable<String> identifications, CancellationToken cancellationToken = default);
    }
}
