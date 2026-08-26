using Connection360.Application.DTOs;
using Connection360.Domain.Dtos;

namespace Connection360.Application.Ports
{
    public interface IGetUserManagementUseCase
    {
        public Task<IList<Auth0UserDto>> GetAllUsersAsync(UsersManagementRequest usersManagement);
        public Task<Boolean> UpdateUserAsync(String userId, Auth0UserDto userUpdate);
        public Task<Auth0UserDto> GetUsersByIdAsync(String userId);
        public Task<Boolean> DeleteUserAsync(String userId);
    }
}
