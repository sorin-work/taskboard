using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Taskboard.Api.Common;

namespace Taskboard.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out var id)
            ? id
            : throw new UnauthorizedException("You need to sign in.");
    }
}
