using System.Security.Claims;

namespace PersonaNest.Web.Extensions;

/// <summary>
/// Reads the signed-in user's id from the cookie's claims. Avoids calling
/// <c>UserManager.GetUserAsync</c> in every action, which would hit the database for something
/// already present in the ticket.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>The signed-in user's id, or null when anonymous.</summary>
    public static string? GetUserId(this ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.NameIdentifier);

    public static bool IsSignedIn(this ClaimsPrincipal principal)
        => principal.Identity?.IsAuthenticated == true;
}
