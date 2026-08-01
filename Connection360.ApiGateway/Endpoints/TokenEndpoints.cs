using Connection360.ApiGateway.Configuration;
using Connection360.ApiGateway.Entitys;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Connection360.ApiGateway.Endpoints
{
    public static class TokenEndpoints
    {
        public static IEndpointRouteBuilder MapTokenEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/connect/token", (TokenRequest request, JwtSettings jwtSettings) =>
            {
                // Client Credentials Grant: valida client_id/client_secret (en produccion: contra un store seguro,
                // hash de secretos, no comparacion de texto plano como aqui simplificado con fines demostrativos).
                if (request.GrantType != "client_credentials")
                    return Results.BadRequest(new { error = "unsupported_grant_type" });

                if (!IsValidClient(request.ClientId, request.ClientSecret, out String role))
                    return Results.Json(new { error = "invalid_client" }, statusCode: StatusCodes.Status401Unauthorized);

                String token = GenerateJwt(jwtSettings, request.ClientId!, role);

                return Results.Ok(new
                {
                    access_token = token,
                    token_type = "Bearer",
                    expires_in = jwtSettings.AccessTokenMinutes * 60
                });
            })
            .RequireRateLimiting("AuthPolicy") // proteccion anti fuerza-bruta especifica para este endpoint
            .WithName("IssueToken")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

            return app;
        }

        private static Boolean IsValidClient(String? clientId, String? clientSecret, out String role)
        {
            // DEMO: reemplazar por validacion contra base de datos / Key Vault con secretos hasheados (BCrypt/Argon2).
            Dictionary<String, (String Secret, String Role)> validClients = new Dictionary<String, (String Secret, String Role)>
            {
                ["admin-client"] = ("S3cureAdminSecret!", "Admin"),
                ["editor-client"] = ("S3cureEditorSecret!", "Editor"),
                ["reader-client"] = ("S3cureReaderSecret!", "Reader")
            };

            if (clientId is not null && validClients.TryGetValue(clientId, out var entry) && entry.Secret == clientSecret)
            {
                role = entry.Role;
                return true;
            }

            role = String.Empty;
            return false;
        }

        private static String GenerateJwt(JwtSettings settings, String clientId, String role)
        {
            string roleCustomClaimType = "https://mi-app.com/claims/roles";
            IEnumerable<String> roles = new List<String> { "ADMIN", "CLIENT", "ANALISTAOPE", "ANALISTASAC"};
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, clientId),
                //new Claim(ClaimTypes.Role, role),
                //new Claim(roleCustomClaimType, "Admin"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Añade cada rol con la clave del Custom Claim de Auth0
            foreach (var roleAdd in roles)
            {
                claims.Add(new Claim(roleCustomClaimType, roleAdd));
            }

            SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret));
            SigningCredentials credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken token = new JwtSecurityToken(
                issuer: settings.Issuer,
                audience: settings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(settings.AccessTokenMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
