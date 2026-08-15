using PersonaNest.Domain.Enums;

namespace PersonaNest.Domain.Entities;

/// <summary>
/// A real-time notification delivered to <see cref="Recipient"/> (Phase 15, SignalR). Fully
/// denormalized - <see cref="Message"/> and <see cref="Url"/> are pre-rendered at creation time -
/// so the notification list never needs to join back into Entry/Comment/Media, matching the
/// project's DTO-denormalization pattern (see CommentDto's Author* fields).
/// </summary>
public class Notification
{
    public int Id { get; set; }

    public string RecipientUserId { get; set; } = string.Empty;
    public ApplicationUser Recipient { get; set; } = null!;

    /// <summary>The user who caused this notification. Null is reserved for future system notices.</summary>
    public string? ActorUserId { get; set; }
    public ApplicationUser? Actor { get; set; }

    public NotificationType Type { get; set; }

    public string Message { get; set; } = string.Empty;

    /// <summary>Relative site URL to navigate to when the notification is clicked.</summary>
    public string? Url { get; set; }

    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
