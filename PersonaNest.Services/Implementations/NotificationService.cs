using PersonaNest.Domain.Abstractions;
using PersonaNest.Domain.Entities;
using PersonaNest.Domain.Enums;
using PersonaNest.Services.Common;
using PersonaNest.Services.DTOs.Responses;
using PersonaNest.Services.Interfaces;
using PersonaNest.Services.Mapping;

namespace PersonaNest.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationBroadcaster _broadcaster;

    public NotificationService(IUnitOfWork uow, INotificationBroadcaster broadcaster)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        _broadcaster = broadcaster ?? throw new ArgumentNullException(nameof(broadcaster));
    }

    public async Task<PagedResult<NotificationDto>> GetForUserAsync(
        string userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var repository = _uow.Notifications;

        var items = await repository.ListAsync(
            n => n.RecipientUserId == userId,
            NotificationMappings.ToDto,
            q => q.OrderByDescending(n => n.CreatedAt),
            page, pageSize, cancellationToken);

        var total = await repository.CountAsync(n => n.RecipientUserId == userId, cancellationToken);

        return new PagedResult<NotificationDto>(items, total, page, pageSize);
    }

    public Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default)
        => _uow.Notifications.CountAsync(n => n.RecipientUserId == userId && !n.IsRead, cancellationToken);

    public async Task<ServiceResult> MarkAsReadAsync(
        int notificationId, string userId, CancellationToken cancellationToken = default)
    {
        var repository = _uow.Notifications;
        var notification = await repository.GetByIdAsync(notificationId, cancellationToken);

        if (notification is null || notification.RecipientUserId != userId)
        {
            return ServiceResult.Failure("That notification no longer exists.");
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            repository.Update(notification);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> MarkAllAsReadAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        await _uow.Notifications.MarkAllAsReadAsync(userId, cancellationToken);
        return ServiceResult.Success();
    }

    public async Task NotifyNewFollowerAsync(
        string actorId, string recipientId, CancellationToken cancellationToken = default)
    {
        if (actorId == recipientId)
        {
            return;
        }

        var actor = await GetActorAsync(actorId, cancellationToken);
        if (actor is null)
        {
            return;
        }

        await CreateAndBroadcastAsync(
            recipientId, actorId, actor, NotificationType.NewFollower,
            $"{actor.DisplayName} started following you.",
            $"/Profile/{actor.UserName}",
            cancellationToken);
    }

    public async Task NotifyEntryLikedAsync(
        string actorId, int entryId, CancellationToken cancellationToken = default)
    {
        var entry = await GetEntryOwnerAsync(entryId, cancellationToken);
        if (entry is null || entry.UserId == actorId)
        {
            return;
        }

        var actor = await GetActorAsync(actorId, cancellationToken);
        if (actor is null)
        {
            return;
        }

        await CreateAndBroadcastAsync(
            entry.UserId, actorId, actor, NotificationType.EntryLiked,
            $"{actor.DisplayName} liked your entry for {entry.MediaTitle}.",
            $"/Entries/Details/{entryId}",
            cancellationToken);
    }

    public async Task NotifyNewCommentAsync(
        string actorId, int entryId, CancellationToken cancellationToken = default)
    {
        var entry = await GetEntryOwnerAsync(entryId, cancellationToken);
        if (entry is null || entry.UserId == actorId)
        {
            return;
        }

        var actor = await GetActorAsync(actorId, cancellationToken);
        if (actor is null)
        {
            return;
        }

        await CreateAndBroadcastAsync(
            entry.UserId, actorId, actor, NotificationType.NewComment,
            $"{actor.DisplayName} commented on your entry for {entry.MediaTitle}.",
            $"/Entries/Details/{entryId}",
            cancellationToken);
    }

    public async Task NotifyNewReplyAsync(
        string actorId, int parentCommentId, CancellationToken cancellationToken = default)
    {
        var parent = await _uow.Repository<Comment>().FirstOrDefaultAsync(
            c => c.Id == parentCommentId,
            c => new CommentOwnerProjection(c.UserId, c.EntryId),
            cancellationToken);

        if (parent is null || parent.UserId == actorId)
        {
            return;
        }

        var actor = await GetActorAsync(actorId, cancellationToken);
        if (actor is null)
        {
            return;
        }

        await CreateAndBroadcastAsync(
            parent.UserId, actorId, actor, NotificationType.NewReply,
            $"{actor.DisplayName} replied to your comment.",
            $"/Entries/Details/{parent.EntryId}",
            cancellationToken);
    }

    private Task<ActorProjection?> GetActorAsync(string actorId, CancellationToken cancellationToken)
        => _uow.Repository<ApplicationUser>().FirstOrDefaultAsync(
            u => u.Id == actorId,
            u => new ActorProjection(u.DisplayName, u.UserName, u.ProfilePictureUrl),
            cancellationToken);

    private Task<EntryOwnerProjection?> GetEntryOwnerAsync(int entryId, CancellationToken cancellationToken)
        => _uow.Entries.FirstOrDefaultAsync(
            e => e.Id == entryId,
            e => new EntryOwnerProjection(e.UserId, e.Media.Title),
            cancellationToken);

    /// <summary>
    /// Persists the notification, then broadcasts it built from data already in hand rather than
    /// re-querying - the caller already resolved <paramref name="actor"/> to compose the message.
    /// </summary>
    private async Task CreateAndBroadcastAsync(
        string recipientId, string actorId, ActorProjection actor, NotificationType type,
        string message, string url, CancellationToken cancellationToken)
    {
        var notification = new Notification
        {
            RecipientUserId = recipientId,
            ActorUserId = actorId,
            Type = type,
            Message = message,
            Url = url,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Notifications.AddAsync(notification, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var dto = new NotificationDto
        {
            Id = notification.Id,
            Type = type,
            Message = message,
            Url = url,
            ActorDisplayName = actor.DisplayName,
            ActorProfilePictureUrl = actor.ProfilePictureUrl,
            IsRead = false,
            CreatedAt = notification.CreatedAt
        };

        await _broadcaster.BroadcastAsync(recipientId, dto, cancellationToken);
    }

    // Internal rather than private purely for test mockability (IRepository<T>.FirstOrDefaultAsync
    // is generic on TResult, so a test needs to name this type to set up a matching mock call) -
    // same pattern as TasteProfileCalculator.TasteEntryRow (Phase 13, see the csproj's
    // InternalsVisibleTo).
    internal sealed record ActorProjection(string DisplayName, string? UserName, string? ProfilePictureUrl);

    internal sealed record EntryOwnerProjection(string UserId, string MediaTitle);

    internal sealed record CommentOwnerProjection(string UserId, int EntryId);
}
