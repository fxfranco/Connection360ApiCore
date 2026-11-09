using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Connection360Notification.Api.Tests.TestSupport
{
    /// <summary>
    /// Crea un <see cref="ControllerContext"/> con un usuario autenticado que posee los roles indicados.
    /// Los controladores consultan <c>User.IsInRole(...)</c>, por lo que sin un HttpContext lanzarian NullReferenceException.
    /// </summary>
    internal static class ControllerContextFactory
    {
        public static ControllerContext Create(params String[] roles)
        {
            IEnumerable<Claim> claims = roles.Select(role => new Claim(ClaimTypes.Role, role));
            var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");

            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
        }
    }
}
