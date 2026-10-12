using Connection360.Etl.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Connection360.Etl.Domain.Ports.Persistence
{
    /// <summary>
    /// Puerto secundario (saliente): consulta los canales de notificación de los clientes
    /// (connection360write.customer_notification_channels, relacionada con
    /// connection360write.customers por id_customer). Lo implementa Connection360.Etl.Infrastructure.
    /// </summary>
    public interface ICustomerNotificationChannelsRepository
    {
        /// <summary>
        /// Devuelve, en una sola consulta, los canales de notificación de los clientes indicados
        /// (la identificación equivale al NitCliente). Un cliente que no exista o que no tenga
        /// configuración de canales simplemente NO aparece en el resultado: quien llama debe usar
        /// <see cref="CustomerNotificationChannelSettings.None"/> para esos casos.
        /// </summary>
        Task<IReadOnlyDictionary<String, CustomerNotificationChannelSettings>> GetByCustomerIdentificationsAsync(
            IEnumerable<String> identifications, CancellationToken cancellationToken = default);
    }
}
