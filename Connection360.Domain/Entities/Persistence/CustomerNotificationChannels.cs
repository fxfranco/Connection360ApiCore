namespace Connection360.Domain.Entities.Persistence
{
    public class CustomerNotificationChannels
    {
        public Int64 IdNotificationChannel { get; private set; }
        public Int64 IdCustomer { get; private set; }
        public Boolean Application { get; private set; }
        public Boolean Email { get; private set; }
        public Boolean TextMessages { get; private set; }

        private CustomerNotificationChannels(){ }

        public CustomerNotificationChannels(Int64 idNotificationChannels, Int64 idCustomer, Boolean application, Boolean email, Boolean textMessages)
        {
            IdNotificationChannel = idNotificationChannels;
            Update(idCustomer, application, email, textMessages);
        }

        public void Update(Int64 idCustomer, Boolean application, Boolean email, Boolean textMessages)
        {
            if (idCustomer <= 0)
                throw new ArgumentException("El id del cliente no puede estar vacío.", nameof(idCustomer));

            IdCustomer = idCustomer;
            Application = application;
            Email = email;
            TextMessages = textMessages;
        }

    }
}
