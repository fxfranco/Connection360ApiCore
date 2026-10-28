using System;
using System.Collections.Generic;
using System.Text;

namespace Connection360.Application.DTOs.Persistence
{
    /// <summary>
    /// Configuración de eventos de notificación de un cliente, ya persistida.
    /// </summary>
    /// <param name="IdNotificationEvent">Identificador del registro de eventos de notificación.</param>
    /// <param name="IdCustomer">Identificador del cliente dueño de la configuración.</param>
    /// <param name="ChangeState">Notificar ante cambios de estado del envío.</param>
    /// <param name="SuccessfulDelivery">Notificar cuando el envío se entrega exitosamente.</param>
    /// <param name="WithIssues">Notificar cuando el envío presenta novedades.</param>
    /// <param name="ShipmentTransit">Notificar cuando el envío inicia tránsito.</param>
    /// <param name="DeliveryReminder">Notificar como recordatorio de entrega próxima.</param>
    public record CustomerNotificationEventsResponseDto(Int64 IdNotificationEvent, Int64 IdCustomer, Boolean ChangeState, Boolean SuccessfulDelivery, Boolean WithIssues, Boolean ShipmentTransit, Boolean DeliveryReminder);
}
