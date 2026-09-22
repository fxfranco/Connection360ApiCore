namespace Connection360Notification.Application.DTOs
{
    public class ClientSummaryRequest
    {
        //TODO: Pendiente cambiar nombre para no confundir 
        public String IdClient { get; set; } = default!;
        public String RoleName { get; set; } = default!;
        public String FilterValue { get; set; } = default!;
        public String IdQueryClient { get; set; } = default!;
        public Boolean AllClient {  get; set; } = default!;
    }
}
