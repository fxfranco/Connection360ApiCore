using System;
using System.Collections.Generic;
using System.Text;

namespace Connection360.Application.DTOs.Persistence
{
    public record CustomerNotificationEventsResponseDto(Int64 IdNotificationEvent, Int64 IdCustomer, Boolean ChangeState, Boolean SuccessfulDelivery, Boolean WithIssues, Boolean ShipmentTransit, Boolean DeliveryReminder);
}