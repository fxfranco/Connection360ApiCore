using Connection360.Application.DTOs;
using Connection360.Application.Enum;
using Connection360.Application.Ports;

namespace Connection360.Application.UseCases
{
    public class GetNotificationsUseCase : IGetNotificationsUseCase
    {
        public List<NotificationsListResponse> ExecuteGetNotificationsAllAsync(ClientSummaryRequest request, CancellationToken cancellationToken)
        {

            //Todo: Pendiente realizar toda la consulta de notificaciones reales

            List<NotificationsListResponse> notif = new List<NotificationsListResponse>
            {
                new NotificationsListResponse
                {
                    IdNotification = 1,
                    NotificationType = NotificationType.ChangeState,
                    DocumentNumber = "HBL-5U6HC36K",
                    Title = "Cambio de estado a pendiente.",                    
                    Message = "Se registra el envío en el sistema, queda pendiente de procesamiento.",
                    MessageDate = DateTime.Now.AddDays(-3),
                    NotificationStatus = NotificationStatus.Read,
                    NotificationDate = DateTime.Now
                },
                new NotificationsListResponse
                {
                    IdNotification = 2,
                    NotificationType = NotificationType.ChangeState,
                    DocumentNumber = "HBL-5U6HC36K",
                    Title = "Cambio de estado a en tránsito.",
                    Message = "Se confirma recolección de la carga, inicia tránsito internacional.",
                    MessageDate = DateTime.Now.AddDays(-3),
                    NotificationStatus = NotificationStatus.Read,
                    NotificationDate = DateTime.Now
                },
                new NotificationsListResponse
                {
                    IdNotification = 3,
                    NotificationType = NotificationType.ChangeState,
                    DocumentNumber = "HBL-5U6HC36K",
                    Title = "Cambio de estado a en aduana origen",
                    Message = "Se inicia proceso de nacionalización en aduana de origen.",
                    MessageDate = DateTime.Now.AddDays(-3),
                    NotificationStatus = NotificationStatus.Read,
                    NotificationDate = DateTime.Now
                },
                new NotificationsListResponse
                {
                    IdNotification = 4,
                    NotificationType = NotificationType.ChangeState,
                    DocumentNumber = "HBL-5U6HC36K",
                    Title = "Cambio de estado a en aduana destino",
                    Message = "El envío llegó a la aduana de destino para trámite de importación.",
                    MessageDate = DateTime.Now.AddDays(-3),
                    NotificationStatus = NotificationStatus.Read,
                    NotificationDate = DateTime.Now
                },
                new NotificationsListResponse
                {
                    IdNotification = 5,
                    NotificationType = NotificationType.ChangeState,
                    DocumentNumber = "HBL-5U6HC36K",
                    Title = "Cambio de estado a novedad",
                    Message = "Se presenta una novedad documental que retrasa el proceso.",
                    MessageDate = DateTime.Now.AddDays(-3),
                    NotificationStatus = NotificationStatus.Unread,
                    NotificationDate = DateTime.Now
                },
                new NotificationsListResponse
                {
                    IdNotification = 6,
                    NotificationType = NotificationType.ChangeState,
                    DocumentNumber = "HBL-5U6HC36K",
                    Title = "Cambio de estado a entrado",
                    Message = "El envío fue entregado satisfactoriamente al destinatario final.",
                    MessageDate = DateTime.Now.AddDays(-3),
                    NotificationStatus = NotificationStatus.Unread,
                    NotificationDate = DateTime.Now
                },
                new NotificationsListResponse
                {
                    IdNotification = 7,
                    NotificationType = NotificationType.Comment,
                    DocumentNumber = "HBL-5U6HC36K",
                    Title = "Comentario del transportista",
                    Message = "Su pedido es demasiado pesado segùn indicaciones",
                    MessageDate = DateTime.Now.AddDays(-3),
                    NotificationStatus = NotificationStatus.Unread,
                    NotificationDate = DateTime.Now
                },
                new NotificationsListResponse
                {
                    IdNotification = 8,
                    NotificationType = NotificationType.Comment,
                    DocumentNumber = "HBL-5U6HC36K",
                    Title = "Comentario de la aduana",
                    Message = "En el momento presentamos muchas demoras y su pedido puede tardar más tiempo",
                    MessageDate = DateTime.Now.AddDays(-3),
                    NotificationStatus = NotificationStatus.Unread,
                    NotificationDate = DateTime.Now
                },
            };

            return notif;
        }
    }
}
