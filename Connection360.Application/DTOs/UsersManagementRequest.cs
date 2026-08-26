namespace Connection360.Application.DTOs
{
    public class UsersManagementRequest
    {
        public String UserId { get; set; } = default!;
        public String RoleName { get; set; } = default!;
        public Int32 Page { get; set; } = default!;
        public Int32 Size { get; set; } = default!;
    }
}
