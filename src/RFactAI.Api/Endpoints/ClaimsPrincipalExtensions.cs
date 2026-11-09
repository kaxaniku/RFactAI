using System.Security.Claims;

namespace RFactAI.Api.Endpoints;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The authenticated user's id from the JWT "sub" claim, or null for anonymous callers.</summary>
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true) return null;

        var value = principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
