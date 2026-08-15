using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PersonaNest.Web.Hubs;

/// <summary>
/// Server-to-client push only (§12 progressive enhancement - the notification bell and full
/// history page both work from a plain server render; this hub only makes updates arrive live
/// on top of that). No client-invokable methods are needed as a result.
/// <para>
/// <c>Context.UserIdentifier</c> resolves via SignalR's default <c>IUserIdProvider</c>, which
/// reads <c>ClaimTypes.NameIdentifier</c> from the same cookie-auth <see cref="System.Security.Claims.ClaimsPrincipal"/>
/// every controller already reads through <c>ClaimsPrincipalExtensions.GetUserId</c> - so
/// <c>Clients.User(userId)</c> in <see cref="Realtime.SignalRNotificationBroadcaster"/> reaches
/// exactly the right connections with no extra plumbing.
/// </para>
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
}
