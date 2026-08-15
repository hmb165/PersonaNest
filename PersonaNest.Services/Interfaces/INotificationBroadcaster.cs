using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.Interfaces;

/// <summary>
/// Pushes a freshly created notification to the recipient's live connections, if any are open.
/// <para>
/// Implemented in PersonaNest.Web (over SignalR) so the Services layer never references
/// ASP.NET Core hosting/SignalR types - matching the four-layer rule that hosting concerns stay
/// in Web. A no-op implementation would be equally valid here (e.g. in tests); the notification
/// itself is always persisted regardless of whether anyone is listening live.
/// </para>
/// </summary>
public interface INotificationBroadcaster
{
    Task BroadcastAsync(
        string recipientUserId, NotificationDto notification, CancellationToken cancellationToken = default);
}
