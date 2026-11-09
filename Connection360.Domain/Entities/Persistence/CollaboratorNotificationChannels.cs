namespace Connection360.Domain.Entities.Persistence
{
    /// <summary>
    /// Canales de notificación habilitados para un colaborador (roles ADMIN, ANALISTAOPE y ANALISTASAC).
    /// Equivale a <see cref="CustomerNotificationChannels"/> pero asociado a la tabla
    /// <c>connection360write.collaborator_notification_channels</c>.
    /// </summary>
    public class CollaboratorNotificationChannels
    {
        public Int64 IdNotificationChannel { get; private set; }
        public Int64 IdCollaborator { get; private set; }
        public Boolean Application { get; private set; }
        public Boolean Email { get; private set; }
        public Boolean TextMessages { get; private set; }

        private CollaboratorNotificationChannels() { }

        public CollaboratorNotificationChannels(Int64 idNotificationChannel, Int64 idCollaborator, Boolean application, Boolean email, Boolean textMessages)
        {
            IdNotificationChannel = idNotificationChannel;
            Update(idCollaborator, application, email, textMessages);
        }

        public void Update(Int64 idCollaborator, Boolean application, Boolean email, Boolean textMessages)
        {
            if (idCollaborator <= 0)
                throw new ArgumentException("El id del colaborador no puede estar vacío.", nameof(idCollaborator));

            IdCollaborator = idCollaborator;
            Application = application;
            Email = email;
            TextMessages = textMessages;
        }
    }
}
