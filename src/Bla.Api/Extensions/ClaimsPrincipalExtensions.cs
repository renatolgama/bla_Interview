using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Bla.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    // The user id always comes from the validated token's "sub" claim —
    // never from a request body or route — so a user cannot act as someone else.
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(sub, out var userId)
            ? userId
            : throw new InvalidOperationException(
                "The authenticated principal does not carry a valid 'sub' claim.");
    }
}
