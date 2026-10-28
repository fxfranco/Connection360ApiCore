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

        public async Task<Boolean> UpdateUserAsync(String userId, Auth0UserDto userUpdate)
        {
            return await _auth0UserService.UpdateUserAsync(userId, userUpdate);
        }

        public async Task<Boolean> DeleteUserAsync(String userId)
        {

            return await _auth0UserService.DeleteUserAsync(userId);
        }

        public async Task<Auth0UserDto> GetUsersByIdAsync(String userId)
        {
            return await _auth0UserService.GetUsersByIdAsync(userId);
        }

        public async Task<IList<Auth0RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
        {
            return await _auth0UserService.GetRolesAsync(cancellationToken);
        }

        /// <summary>
        /// Activa o inactiva un usuario. En Auth0 "inactivo" equivale a "bloqueado", por eso
        /// <paramref name="isActive"/> se invierte al llamar al servicio.
        /// </summary>
        public async Task<Boolean> SetUserStatusAsync(String userId, Boolean isActive)
        {
            return await _auth0UserService.SetUserBlockedAsync(userId, isBlocked: !isActive);
        }
    }
}
