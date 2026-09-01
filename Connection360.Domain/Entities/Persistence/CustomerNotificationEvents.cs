namespace Connection360.Domain.Entities.Persistence
{
    public class CustomerNotificationEvents
    {
        public Int64 IdNotificationEvent { get; private set; }
        public Int64 IdCustomer { get; private set; }
        public Boolean ChangeState { get; private set; }
        public Boolean SuccessfulDelivery { get; private set; }
        public Boolean WithIssues { get; private set; }
        public Boolean ShipmentTransit { get; private set; }
        public Boolean DeliveryReminder { get; private set; }

        private CustomerNotificationEvents() { }

        public CustomerNotificationEvents(Int64 idNotificationEvent, Int64 idCustomer, Boolean changeState, Boolean successfulDelivery, Boolean withIssues, Boolean shipmentTransit, Boolean deliveryReminder)
        {
            IdNotificationEvent = idNotificationEvent;
            Update(idCustomer, changeState, successfulDelivery, withIssues, shipmentTransit, deliveryReminder);
        }

        public void Update(Int64 idCustomer, Boolean changeState, Boolean successfulDelivery, Boolean withIssues, Boolean shipmentTransit, Boolean deliveryReminder)
        {
            if (idCustomer <= 0)
                throw new ArgumentException("El id del cliente no puede estar vacío.", nameof(idCustomer));

            IdCustomer = idCustomer;
            ChangeState = changeState;
            SuccessfulDelivery = successfulDelivery;
            WithIssues = withIssues;
            ShipmentTransit = shipmentTransit;
            DeliveryReminder = deliveryReminder;
        }
    }
}
