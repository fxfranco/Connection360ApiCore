using Connection360.Etl.Domain.Entities;
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
    /// Adaptador de <see cref="ICustomerNotificationChannelsRepository"/>: consulta en PostgreSQL
    /// connection360write.customers JOIN connection360write.customer_notification_channels (ver
    /// Documents/scriptcustomers.sql y Documents/scriptcustomer_notification_channels.sql).
    /// </summary>
    public class CustomerNotificationChannelsRepository : ICustomerNotificationChannelsRepository
    {
        private readonly DbSession _session;

        public CustomerNotificationChannelsRepository(DbSession session)
        {
            _session = session;
        }

        public async Task<IReadOnlyDictionary<String, CustomerNotificationChannelSettings>> GetByCustomerIdentificationsAsync(
            IEnumerable<String> identifications, CancellationToken cancellationToken = default)
        {
            var identificationsList = identifications?
                .Where(i => !String.IsNullOrWhiteSpace(i))
                .Distinct(StringComparer.Ordinal)
                .ToList()
                ?? new List<String>();

            if (identificationsList.Count == 0)
                return new Dictionary<String, CustomerNotificationChannelSettings>(StringComparer.Ordinal);

            await _session.EnsureConnectionOpenAsync(cancellationToken);

            // Un solo round-trip para todos los clientes del lote (= ANY), solo las 4 columnas
            // necesarias. DISTINCT ON garantiza una fila por cliente aunque la tabla de canales
            // tuviera más de un registro para él (se toma el de menor id_notification_channel).
            // AS con el nombre exacto de la propiedad porque Connection360.Etl.App no habilita
            // Dapper.DefaultTypeMap.MatchNamesWithUnderscores.
            const string query = @"
                SELECT DISTINCT ON (c.identificacion)
                       c.identificacion AS ""Identification"",
                       n.application AS ""Application"",
                       n.email AS ""Email"",
                       n.text_messages AS ""TextMessages""
                FROM connection360write.customers c
                INNER JOIN connection360write.customer_notification_channels n ON n.id_customer = c.id_customer
                WHERE c.identificacion = ANY(@Identifications)
                ORDER BY c.identificacion, n.id_notification_channel;";

            var command = new CommandDefinition(
                query,
                new { Identifications = identificationsList },
                transaction: _session.Transaction,
                cancellationToken: cancellationToken);

            var rows = await _session.Connection.QueryAsync<ChannelsProjection>(command);

            return rows.ToDictionary(
                r => r.Identification,
                r => new CustomerNotificationChannelSettings(r.Application, r.Email, r.TextMessages),
                StringComparer.Ordinal);
        }

        // Proyección interna únicamente para el mapeo de Dapper: no cruza el puerto de dominio.
        private sealed class ChannelsProjection
        {
            public String Identification { get; set; } = String.Empty;
            public Boolean Application { get; set; }
            public Boolean Email { get; set; }
            public Boolean TextMessages { get; set; }
        }
    }
}
