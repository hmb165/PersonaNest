namespace PersonaNest.Domain.Enums;

/// <summary>What triggered a Notification. Phase 15 (SignalR real-time notifications).</summary>
public enum NotificationType
{
    NewFollower = 0,
    EntryLiked = 1,
    NewComment = 2,
    NewReply = 3
}
