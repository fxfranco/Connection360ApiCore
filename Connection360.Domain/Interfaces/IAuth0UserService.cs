using Connection360.Domain.Dtos;
namespace Connection360.Domain.Interfaces
{
    public interface IAuth0UserService
    {
        Task<IList<Auth0UserDto>> GetUsersAsync(Int32 page, Int32 size);
        Task<Boolean> UpdateUserAsync(String userId, Auth0UserDto updateUser);
        Task<Auth0UserDto> GetUsersByIdAsync(String userId);
        Task<Boolean> DeleteUserAsync(String userId);
    }
}
