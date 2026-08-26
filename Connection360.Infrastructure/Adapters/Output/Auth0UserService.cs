using Auth0.Core.Exceptions;
using Auth0.ManagementApi;
using Connection360.Domain.Dtos;
using Connection360.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Drawing;

namespace Connection360.Infrastructure.Adapters.Output
{
    public class Auth0UserService : IAuth0UserService
    {
        private readonly IConfiguration _config;

        public Auth0UserService(IConfiguration config)
        {
            _config = config;
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

        public async Task<IList<Auth0UserDto>> GetUsersAsync(Int32 page, Int32 size)
        {
            try
            {
                var client = GetManagementApiClientAsync();

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

                List<Auth0UserDto> users = response.CurrentPage.Items.Select<UserResponseSchema, Auth0UserDto>(user => new Auth0UserDto
                {
                    UserId = user.UserId ?? String.Empty,
                    Email = user.Email ?? String.Empty,
                    UserName = user.Name ?? String.Empty,
                    PhoneNumber = user.PhoneNumber ?? String.Empty,
                    CreatedDate = (DateTime)(user.CreatedAt ?? DateTime.MinValue),
                    UpdatedDate = (DateTime)(user.UpdatedAt ?? DateTime.MinValue),
                    Nickname = user.Nickname ?? String.Empty,
                    IsBlocked = (Boolean)(user.Blocked ?? false)
                }).ToList();

                return users;
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

        public async Task<Boolean> UpdateUserAsync(String userId, Auth0UserDto auth0User)
        {
            Boolean isSucceded = false;
            try
            {
                var client = GetManagementApiClientAsync();

                UpdateUserRequestContent updateRequest = new UpdateUserRequestContent();

                if (!String.IsNullOrWhiteSpace(auth0User.UserName))
                {
                    updateRequest.Username = auth0User.UserName;
                }

                if (!String.IsNullOrWhiteSpace(auth0User.Email))
                {
                    updateRequest.Email = auth0User.Email;
                }

                if (!String.IsNullOrWhiteSpace(auth0User.PhoneNumber))
                {
                    updateRequest.PhoneNumber = auth0User.PhoneNumber;
                }

                if (!String.IsNullOrWhiteSpace(auth0User.Nickname))
                {
                    updateRequest.Nickname = auth0User.Nickname;
                }

                if (auth0User.IsBlocked != null)
                {
                    updateRequest.Blocked = auth0User.IsBlocked;
                }

                UpdateUserResponseContent userResponse = await client.Users.UpdateAsync(userId, updateRequest);
                isSucceded = (userResponse != null);
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
            return isSucceded;
        }

        public async Task<Auth0UserDto> GetUsersByIdAsync(String userId)
        {
            try
            {
                var client = GetManagementApiClientAsync();

                //GetUserRequestParameters requestParams = new GetUserRequestParameters
                //{
                //    Fields = "user_id",
                //    IncludeFields = false
                //};

                var userrResponse = await client.Users.GetAsync(userId, new GetUserRequestParameters());

                return new Auth0UserDto
                {
                    UserId = userrResponse.UserId ?? String.Empty,
                    Email = userrResponse.Email ?? String.Empty,
                    UserName = userrResponse.Name ?? String.Empty,
                    PhoneNumber = userrResponse.PhoneNumber ?? String.Empty,
                    CreatedDate = (DateTime)(userrResponse.CreatedAt ?? DateTime.MinValue),
                    UpdatedDate = (DateTime)(userrResponse.UpdatedAt ?? DateTime.MinValue),
                    Nickname = userrResponse.Nickname ?? String.Empty,
                    IsBlocked = (Boolean)(userrResponse.Blocked ?? false)
                };
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

        public async Task<Boolean> DeleteUserAsync(String userId)
        {
            Boolean isSucceded = false;
            try
            {
                var client = GetManagementApiClientAsync();

                await client.Users.DeleteAsync(userId);
                isSucceded = true;
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
            return isSucceded;
        }
    }
}
