using Connection360.Domain.Dtos;
namespace Connection360.Domain.Interfaces
{
    public interface IAuth0UserService
    {
        Task<IList<Auth0UserDto>> GetUsersAsync(Int32 page, Int32 size);
        Task<Boolean> UpdateUserAsync(String userId, Auth0UserDto updateUser);
        Task<Auth0UserDto> GetUsersByIdAsync(String userId);
        Task<Boolean> DeleteUserAsync(String userId);

        /// <summary>Lista los roles definidos en Auth0 (id, nombre y descripción).</summary>
        Task<IList<Auth0RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Activa (<paramref name="isBlocked"/> = <c>false</c>) o inactiva (<c>true</c>) un usuario,
        /// bloqueándolo o desbloqueándolo en Auth0.
        /// </summary>
        Task<Boolean> SetUserBlockedAsync(String userId, Boolean isBlocked);

        /// <summary>
        /// Lista todos los usuarios de Auth0 extrayendo solo los datos básicos necesarios para
        /// notificar (id, identificación, correo y teléfono). Lo usa el worker de sincronización.
        /// </summary>
        Task<IList<Auth0UserBasicDto>> GetBasicUsersAsync(CancellationToken cancellationToken = default);
    }
}
