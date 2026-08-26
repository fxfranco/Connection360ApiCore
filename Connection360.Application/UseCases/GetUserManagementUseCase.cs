using Connection360.Application.DTOs;
using Connection360.Application.Ports;
using Connection360.Domain.Dtos;
using Connection360.Domain.Interfaces;

namespace Connection360.Application.UseCases
{
    public class GetUserManagementUseCase : IGetUserManagementUseCase
    {
        private readonly IAuth0UserService _auth0UserService;

        public GetUserManagementUseCase(IAuth0UserService auth0UserService)
        {
            _auth0UserService = auth0UserService;
        }

        public async Task<IList<Auth0UserDto>> GetAllUsersAsync(UsersManagementRequest usersManagement)
        {
            return await _auth0UserService.GetUsersAsync(page: usersManagement.Page, size: usersManagement.Size);
        }

        public async Task<Boolean> UpdateUserAsync(String userId, Auth0UserDto dto)
        {
            return await _auth0UserService.UpdateUserAsync(userId, dto);
        }

        public async Task<Boolean> DeleteUserAsync(String userId)
        {

            return await _auth0UserService.DeleteUserAsync(userId);
        }

        public async Task<Auth0UserDto> GetUsersByIdAsync(String userId)
        {
            return await _auth0UserService.GetUsersByIdAsync(userId);
        }
    }
}
