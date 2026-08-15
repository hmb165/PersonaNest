using PersonaNest.Domain.Abstractions;
using PersonaNest.Services.Common;
using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.Interfaces;

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetForUserAsync(
        string userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);

    Task<ServiceResult> MarkAsReadAsync(
        int notificationId, string userId, CancellationToken cancellationToken = default);

    Task<ServiceResult> MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Someone followed <paramref name="recipientId"/>.</summary>
    Task NotifyNewFollowerAsync(
        string actorId, string recipientId, CancellationToken cancellationToken = default);

    /// <summary>Someone liked an entry. No-op if the entry no longer exists or was self-liked.</summary>
    Task NotifyEntryLikedAsync(
        string actorId, int entryId, CancellationToken cancellationToken = default);

    /// <summary>Someone posted a top-level comment on an entry. No-op if self-commented.</summary>
    Task NotifyNewCommentAsync(
        string actorId, int entryId, CancellationToken cancellationToken = default);

    /// <summary>Someone replied to a comment. No-op if replying to their own comment.</summary>
    Task NotifyNewReplyAsync(
        string actorId, int parentCommentId, CancellationToken cancellationToken = default);
}
