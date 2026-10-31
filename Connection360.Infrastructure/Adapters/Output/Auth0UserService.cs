using Auth0.Core.Exceptions;
using Auth0.ManagementApi;
using Auth0.ManagementApi.Users;
using Connection360.Domain.Dtos;
using Connection360.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Connection360.Infrastructure.Adapters.Output
{
    public class Auth0UserService : IAuth0UserService
    {
        /// <summary>Cantidad máxima de consultas simultáneas de rol contra la Management API al listar usuarios.</summary>
        private const Int32 MaxConcurrentRoleLookups = 5;

        /// <summary>Usuarios por página al sincronizar (máximo permitido por Auth0: 100).</summary>
        private const Int32 BasicUsersPageSize = 100;

        /// <summary>
        /// Auth0 solo permite recuperar los primeros 1000 resultados de <c>GET /api/v2/users</c>
        /// (página * por_página &lt;= 1000), por lo que se detiene la paginación en ese tope.
        /// </summary>
        private const Int32 MaxSearchPages = 1000 / BasicUsersPageSize;

        private readonly IConfiguration _config;
        private readonly ILogger<Auth0UserService>? _logger;

        public Auth0UserService(IConfiguration config, ILogger<Auth0UserService>? logger = null)
        {
            _config = config;
            _logger = logger;
        }

        private ManagementClient GetManagementApiClientAsync()
        {
            var domain = _config["Auth0Management:Domain"]!;

            return new ManagementClient(new ManagementClientOptions
            {
                Domain = domain,
                TokenProvider = new ClientCredentialsTokenProvider(
                    domain: domain,
                    clientId: _config["Auth0Management:ClientId"]!,
                    clientSecret: _config["Auth0Management:ClientSecret"]!
                )
            });
        }

        /// <summary>
        /// Crea el cliente de la Management API y ejecuta <paramref name="action"/> registrando (y
        /// relanzando) cualquier error, con el mismo criterio que el resto de métodos del servicio.
        /// </summary>
        private async Task<T> ExecuteAsync<T>(Func<ManagementClient, Task<T>> action)
        {
            try
            {
                var client = GetManagementApiClientAsync();
                return await action(client);
            }
            catch (ManagementApiException mex)
            {
                // Errores de la Management API (SDK 10): 401, 403 "Insufficient scope", 404, 429...
                Console.WriteLine($"Error de la Api: {mex.StatusCode} - {mex.Message}");
                throw;
            }
            catch (ErrorApiException aex)
            {
                Console.WriteLine($"Error de la Api: {(Int16)aex.StatusCode} - {aex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inesperado del sistema: {ex.Message}");
                throw;
            }
        }

        public async Task<IList<Auth0UserDto>> GetUsersAsync(Int32 page, Int32 size)
        {
            return await ExecuteAsync<IList<Auth0UserDto>>(async client =>
            {
                //// En v10 se utiliza ListUsersRequestParameters para paginar/filtrar
                var requestParams = new ListUsersRequestParameters
                {
                    Page = page,
                    PerPage = size,
                    IncludeTotals = true,
                    Sort = "Name:1",
                    //Connection = "connection",
                    //Fields = "fields",
                    //IncludeFields = true,
                    //Q = "q",
                    SearchEngine = SearchEngineVersionsEnum.V3,
                    PrimaryOrder = true
                };

                //// Forma 1: Usando GetAllAsync (v10 estándar)
                var response = await client.Users.ListAsync(requestParams);

                List<Auth0UserDto> users = response.CurrentPage.Items.Select<UserResponseSchema, Auth0UserDto>(user =>
                {
                    Auth0UserDto dto = new Auth0UserDto
                    {
                        UserId = user.UserId ?? String.Empty,
                        Email = user.Email ?? String.Empty,
                        UserName = user.Name ?? String.Empty,
                        PhoneNumber = user.PhoneNumber ?? String.Empty,
                        CreatedDate = user.CreatedAt ?? DateTime.MinValue,
                        UpdatedDate = user.UpdatedAt ?? DateTime.MinValue,
                        Nickname = user.Nickname ?? String.Empty,
                        IsBlocked = user.Blocked ?? false
                    };

                    // company, document, fullName y phone.number salen de user_metadata.
                    Auth0UserMetadataReader.Apply(dto, user.UserMetadata);
                    return dto;
                }).ToList();

                // El rol no viene en la respuesta de /users: se consulta por usuario (GET /users/{id}/roles).
                await FillRolesAsync(client, users);

                return users;
            });
        }

        public async Task<Boolean> UpdateUserAsync(String userId, Auth0UserDto updateUser)
        {
            return await ExecuteAsync(async client =>
            {
                // 1. Si se pide un cambio de rol, se verifica que el rol exista ANTES de modificar nada
                //    (GET /roles/{id} responde 404 si no existe), para no dejar el usuario a medio actualizar.
                String? newRoleId = String.IsNullOrWhiteSpace(updateUser.RoleId) ? null : updateUser.RoleId.Trim();
                if (newRoleId != null)
                {
                    await client.Roles.GetAsync(newRoleId);
                }

                // 2. Se consulta el usuario actual para enviar a Auth0 SOLO lo que realmente cambia. El cliente
                //    suele devolver el objeto completo que recibió del GET (con email, teléfono, nombre...
                //    sin modificar); reenviar campos sin cambios hace que Auth0 rechace la petición cuando
                //    la conexión no permite actualizarlos (p. ej. username, nickname o teléfono).
                var current = await client.Users.GetAsync(userId, new GetUserRequestParameters());

                // 3. Correo y teléfono se envían en peticiones SEPARADAS: Auth0 restringe actualizar a la vez
                //    ciertos atributos de identidad (email, phone_number, username, password) junto con otros.
                if (Auth0UserUpdatePlanner.HasChanged(updateUser.Email, current.Email, ignoreCase: true))
                {
                    await client.Users.UpdateAsync(userId, new UpdateUserRequestContent { Email = updateUser.Email!.Trim() });
                }

                if (Auth0UserUpdatePlanner.HasChanged(updateUser.PhoneNumber, current.PhoneNumber, ignoreCase: false))
                {
                    await client.Users.UpdateAsync(userId, new UpdateUserRequestContent { PhoneNumber = updateUser.PhoneNumber!.Trim() });
                }

                // 4. Resto de atributos de perfil, en una sola petición y solo si cambian.
                UpdateUserRequestContent? profileUpdate = BuildProfileUpdate(updateUser, current.Name, current.Username, current.Nickname, current.Blocked);
                if (profileUpdate != null)
                {
                    await client.Users.UpdateAsync(userId, profileUpdate);
                }

                // 5. Datos de negocio de user_metadata (company, document, fullName y phone.number): solo si cambian,
                //    en una petición aparte. Auth0 reemplaza cada propiedad de primer nivel completa, por eso el
                //    objeto phone se reenvía con las demás propiedades que ya tenía (ver Auth0UserMetadataUpdater).
                Dictionary<String, Object>? metadataPatch = Auth0UserMetadataUpdater.BuildPatch(updateUser, current.UserMetadata);
                if (metadataPatch != null)
                {
                    await client.Users.UpdateAsync(userId, new UpdateUserRequestContent { UserMetadata = metadataPatch });
                }

                // 6. Cambio de rol: el usuario queda con el rol nuevo (tolera que no tuviera ninguno).
                if (newRoleId != null)
                {
                    await ReplaceUserRoleAsync(client, userId, newRoleId);
                }

                // Nada que cambiar (igual a lo que ya tiene Auth0): la operación se considera exitosa.
                return true;
            });
        }

        /// <summary>
        /// Arma la petición con los atributos de perfil (username, nickname, bloqueo) que realmente cambian,
        /// o <c>null</c> si ninguno cambia.
        /// </summary>
        private static UpdateUserRequestContent? BuildProfileUpdate(Auth0UserDto updateUser, String? currentName, String? currentUsername, String? currentNickname, Boolean? currentBlocked)
        {
            UpdateUserRequestContent profileUpdate = new UpdateUserRequestContent();
            Boolean hasProfileChanges = false;

            // UserName se expone a partir de "name" (ver GetUsersAsync), por eso un valor igual al
            // nombre o al username actual se considera "sin cambios".
            if (Auth0UserUpdatePlanner.HasChanged(updateUser.UserName, currentName, ignoreCase: false)
                && Auth0UserUpdatePlanner.HasChanged(updateUser.UserName, currentUsername, ignoreCase: false))
            {
                profileUpdate.Username = updateUser.UserName!.Trim();
                hasProfileChanges = true;
            }

            if (Auth0UserUpdatePlanner.HasChanged(updateUser.Nickname, currentNickname, ignoreCase: false))
            {
                profileUpdate.Nickname = updateUser.Nickname!.Trim();
                hasProfileChanges = true;
            }

            if (updateUser.IsBlocked != null && updateUser.IsBlocked != (currentBlocked ?? false))
            {
                profileUpdate.Blocked = updateUser.IsBlocked;
                hasProfileChanges = true;
            }

            return hasProfileChanges ? profileUpdate : null;
        }

        public async Task<Auth0UserDto> GetUsersByIdAsync(String userId)
        {
            return await ExecuteAsync(async client =>
            {
                var userrResponse = await client.Users.GetAsync(userId, new GetUserRequestParameters());

                Auth0UserDto user = new Auth0UserDto
                {
                    UserId = userrResponse.UserId ?? String.Empty,
                    Email = userrResponse.Email ?? String.Empty,
                    UserName = userrResponse.Name ?? String.Empty,
                    PhoneNumber = userrResponse.PhoneNumber ?? String.Empty,
                    CreatedDate = userrResponse.CreatedAt ?? DateTime.MinValue,
                    UpdatedDate = userrResponse.UpdatedAt ?? DateTime.MinValue,
                    Nickname = userrResponse.Nickname ?? String.Empty,
                    IsBlocked = userrResponse.Blocked ?? false
                };

                // company, document, fullName y phone.number salen de user_metadata.
                Auth0UserMetadataReader.Apply(user, userrResponse.UserMetadata);

                Auth0.ManagementApi.Role? assignedRole = await GetAssignedRoleOrNullAsync(client, user.UserId, new ForbiddenWarning());
                user.RoleId = assignedRole?.Id;
                user.RoleName = assignedRole?.Name;

                return user;
            });
        }

        public async Task<Boolean> DeleteUserAsync(String userId)
        {
            return await ExecuteAsync(async client =>
            {
                await client.Users.DeleteAsync(userId);
                return true;
            });
        }

        /// <summary>
        /// Lista los roles de Auth0 (<c>GET /api/v2/roles</c>), recorriendo todas las páginas.
        /// </summary>
        public async Task<IList<Auth0RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync<IList<Auth0RoleDto>>(async client =>
            {
                var requestParams = new ListRolesRequestParameters
                {
                    Page = 0,
                    PerPage = 50,
                    IncludeTotals = true
                };

                var pager = await client.Roles.ListAsync(requestParams, cancellationToken: cancellationToken);

                List<Auth0RoleDto> roles = new List<Auth0RoleDto>();
                await foreach (var page in pager.AsPagesAsync(cancellationToken))
                {
                    foreach (Auth0.ManagementApi.Role role in page.Items)
                    {
                        if (String.IsNullOrEmpty(role.Id))
                        {
                            continue;
                        }

                        roles.Add(new Auth0RoleDto
                        {
                            Id = role.Id,
                            Name = role.Name ?? String.Empty,
                            Description = role.Description
                        });
                    }
                }

                return roles;
            });
        }

        /// <summary>
        /// Bloquea o desbloquea al usuario (<c>PATCH /api/v2/users/{id}</c> con <c>blocked</c>).
        /// </summary>
        public async Task<Boolean> SetUserBlockedAsync(String userId, Boolean isBlocked)
        {
            return await ExecuteAsync(async client =>
            {
                await client.Users.UpdateAsync(userId, new UpdateUserRequestContent { Blocked = isBlocked });
                return true;
            });
        }

        /// <summary>
        /// Lista los usuarios de Auth0 pidiendo solo los campos necesarios (<c>fields</c>) y los
        /// reduce a id, documento, nombre completo, teléfono, correo y rol. Documento, nombre y teléfono salen de
        /// <c>user_metadata</c> (<c>document</c>, <c>fullName</c> y <c>phone.number</c>; ver <see cref="Auth0UserBasicMapper"/>).
        /// El nombre del rol se consulta por usuario (<c>GET /users/{id}/roles</c>) con la misma lógica que
        /// <see cref="GetUsersAsync"/> (<see cref="LookupRolesAsync"/>): requiere el permiso <c>read:roles</c>, pero si Auth0
        /// responde 403 la sincronización continúa con el rol vacío y se registra una advertencia.
        /// </summary>
        public async Task<IList<Auth0UserBasicDto>> GetBasicUsersAsync(CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync<IList<Auth0UserBasicDto>>(async client =>
            {
                var requestParams = new ListUsersRequestParameters
                {
                    Page = 0,
                    PerPage = BasicUsersPageSize,
                    IncludeTotals = true,
                    Sort = "Name:1",
                    Fields = Auth0UserBasicMapper.RequestedFields,
                    IncludeFields = true,
                    SearchEngine = SearchEngineVersionsEnum.V3,
                    PrimaryOrder = true
                };

                var pager = await client.Users.ListAsync(requestParams, cancellationToken: cancellationToken);

                // Se indexa por id: si Auth0 repitiera un usuario entre páginas, no se duplica.
                Dictionary<String, Auth0UserBasicDto> users = new Dictionary<String, Auth0UserBasicDto>(StringComparer.Ordinal);
                Int32 pagesRead = 0;
                await foreach (var page in pager.AsPagesAsync(cancellationToken))
                {
                    foreach (UserResponseSchema user in page.Items)
                    {
                        Auth0UserBasicDto basic = Auth0UserBasicMapper.ToBasicDto(user);
                        if (basic.Id.Length > 0)
                        {
                            users[basic.Id] = basic;
                        }
                    }

                    pagesRead++;
                    if (pagesRead >= MaxSearchPages)
                    {
                        _logger?.LogWarning(
                            "Se alcanzó el tope de {Max} usuarios que Auth0 permite recuperar con GET /api/v2/users; si existen más usuarios no se sincronizarán.",
                            MaxSearchPages * BasicUsersPageSize);
                        break;
                    }
                }

                // El rol tampoco viene en /users: se consulta por usuario, igual que en GetUsersAsync.
                IReadOnlyDictionary<String, Auth0.ManagementApi.Role?> assignedRoles = await LookupRolesAsync(client, users.Keys, cancellationToken);
                Auth0UserBasicMapper.ApplyRoleNames(
                    users.Values,
                    assignedRoles.ToDictionary(entry => entry.Key, entry => entry.Value?.Name, StringComparer.Ordinal));

                return users.Values.ToList();
            });
        }

        /// <summary>
        /// Obtiene el rol asignado al usuario (<c>GET /api/v2/users/{id}/roles</c>), o <c>null</c> si no tiene ninguno.
        /// Si Auth0 devolviera varios, se toma el primero (la aplicación maneja un único rol por usuario).
        /// </summary>
        private static async Task<Auth0.ManagementApi.Role?> GetAssignedRoleAsync(ManagementClient client, String userId)
        {
            var pager = await client.Users.Roles.ListAsync(userId, new ListUserRolesRequestParameters { Page = 0, PerPage = 50 });
            IReadOnlyList<Auth0.ManagementApi.Role> roles = pager.CurrentPage.Items;
            return roles.Count > 0 ? roles[0] : null;
        }

        /// <summary>
        /// Igual que <see cref="GetAssignedRoleAsync"/>, pero si Auth0 responde 403 (el cliente M2M no tiene los
        /// permisos para consultar roles: "Insufficient scope, expected one of: read:users AND read:roles, OR
        /// read:role_members") devuelve <c>null</c> en vez de fallar. Así consultar usuarios sigue funcionando
        /// (con RoleId/RoleName vacíos) y el problema queda registrado una sola vez como advertencia. Cualquier otro
        /// error se propaga. Solo se usa al LEER usuarios; cambiar el rol sí exige los permisos y falla si faltan.
        /// </summary>
        private async Task<Auth0.ManagementApi.Role?> GetAssignedRoleOrNullAsync(ManagementClient client, String userId, ForbiddenWarning warning)
        {
            try
            {
                return await GetAssignedRoleAsync(client, userId);
            }
            catch (Exception ex) when (Auth0ManagementErrors.IsForbidden(ex))
            {
                if (warning.TryClaim())
                {
                    _logger?.LogWarning(
                        "Auth0 rechazó la consulta del rol del usuario ({Message}). Autoriza a la aplicación M2M de Auth0Management en la Management API con los permisos {Scopes}; mientras tanto RoleId y RoleName se devuelven vacíos.",
                        ex.Message, Auth0ManagementErrors.RoleLookupScopes);
                }

                return null;
            }
        }

        /// <summary>Permite registrar la advertencia de permisos una sola vez por operación (aunque se consulten muchos usuarios).</summary>
        private sealed class ForbiddenWarning
        {
            private Int32 _claimed;

            public Boolean TryClaim() => Interlocked.Exchange(ref _claimed, 1) == 0;
        }

        /// <summary>
        /// Consulta el rol asignado a cada usuario (<paramref name="userIds"/>, sin repetidos ni vacíos), con un máximo de
        /// <see cref="MaxConcurrentRoleLookups"/> consultas simultáneas para no saturar el límite de peticiones de la
        /// Management API. Devuelve, por id de usuario, su rol o <c>null</c> si no tiene ninguno o si Auth0 respondió 403
        /// (la advertencia de permisos se registra una sola vez; ver <see cref="GetAssignedRoleOrNullAsync"/>).
        /// </summary>
        private async Task<IReadOnlyDictionary<String, Auth0.ManagementApi.Role?>> LookupRolesAsync(ManagementClient client, IEnumerable<String> userIds, CancellationToken cancellationToken = default)
        {
            using SemaphoreSlim gate = new SemaphoreSlim(MaxConcurrentRoleLookups);
            ForbiddenWarning warning = new ForbiddenWarning();
            ConcurrentDictionary<String, Auth0.ManagementApi.Role?> roles = new ConcurrentDictionary<String, Auth0.ManagementApi.Role?>(StringComparer.Ordinal);

            await Task.WhenAll(userIds
                .Where(id => !String.IsNullOrEmpty(id))
                .Distinct(StringComparer.Ordinal)
                .Select(async id =>
                {
                    await gate.WaitAsync(cancellationToken);
                    try
                    {
                        roles[id] = await GetAssignedRoleOrNullAsync(client, id, warning);
                    }
                    finally
                    {
                        gate.Release();
                    }
                }));

            return roles;
        }

        /// <summary>
        /// Completa <see cref="Auth0UserDto.RoleId"/> y <see cref="Auth0UserDto.RoleName"/> de cada usuario
        /// (ver <see cref="LookupRolesAsync"/>).
        /// </summary>
        private async Task FillRolesAsync(ManagementClient client, IList<Auth0UserDto> users)
        {
            IReadOnlyDictionary<String, Auth0.ManagementApi.Role?> roles = await LookupRolesAsync(client, users.Select(user => user.UserId));

            foreach (Auth0UserDto user in users)
            {
                if (!String.IsNullOrEmpty(user.UserId) && roles.TryGetValue(user.UserId, out Auth0.ManagementApi.Role? assignedRole))
                {
                    user.RoleId = assignedRole?.Id;
                    user.RoleName = assignedRole?.Name;
                }
            }
        }

        /// <summary>
        /// Deja al usuario únicamente con el rol <paramref name="newRoleId"/>: primero lo asigna (si aún no lo
        /// tiene) y después retira los demás, de modo que el usuario nunca queda sin rol a mitad del proceso.
        /// </summary>
        private static async Task ReplaceUserRoleAsync(ManagementClient client, String userId, String newRoleId)
        {
            var pager = await client.Users.Roles.ListAsync(userId, new ListUserRolesRequestParameters { Page = 0, PerPage = 50 });
            List<String> currentRoleIds = pager.CurrentPage.Items
                .Select(role => role.Id ?? String.Empty)
                .Where(id => id.Length > 0)
                .ToList();

            if (!currentRoleIds.Contains(newRoleId))
            {
                await client.Users.Roles.AssignAsync(userId, new AssignUserRolesRequestContent { Roles = new[] { newRoleId } });
            }

            List<String> rolesToRemove = currentRoleIds.Where(id => id != newRoleId).ToList();
            if (rolesToRemove.Count > 0)
            {
                await client.Users.Roles.DeleteAsync(userId, new DeleteUserRolesRequestContent { Roles = rolesToRemove });
            }
        }
    }
}
