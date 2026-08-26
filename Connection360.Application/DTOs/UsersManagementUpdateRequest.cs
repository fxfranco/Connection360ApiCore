namespace Connection360.Application.DTOs
{
    public class UsersManagementUpdateRequest
    {
        public String UserId { get; set; } = String.Empty;
        public String? Email { get; set; } = String.Empty;
        public String? UserName { get; set; } = String.Empty;
        public String? PhoneNumber { get; set; } = String.Empty;
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public Boolean? IsBlocked { get; set; }
    }
}
