using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.ViewModels;

/// <summary>/Notifications (Phase 15) - full history, newest first.</summary>
public sealed class NotificationsViewModel
{
    public IReadOnlyList<NotificationDto> Notifications { get; set; } = Array.Empty<NotificationDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>The navbar bell - unread count plus a short recent list, rendered server-side so it
/// works before (and without) the SignalR connection.</summary>
public sealed class NotificationBellViewModel
{
    public int UnreadCount { get; set; }
    public IReadOnlyList<NotificationDto> Recent { get; set; } = Array.Empty<NotificationDto>();
}
