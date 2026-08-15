using Microsoft.AspNetCore.SignalR;
using PersonaNest.Services.DTOs.Responses;
using PersonaNest.Services.Interfaces;
using PersonaNest.Web.Hubs;

namespace PersonaNest.Web.Realtime;

/// <inheritdoc cref="INotificationBroadcaster"/>
public class SignalRNotificationBroadcaster : INotificationBroadcaster
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationBroadcaster(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    }

    public Task BroadcastAsync(
        string recipientUserId, NotificationDto notification, CancellationToken cancellationToken = default)
        => _hubContext.Clients.User(recipientUserId)
            .SendAsync("ReceiveNotification", notification, cancellationToken);
}
