using PersonaNest.Domain.Entities;

namespace PersonaNest.Domain.Abstractions;

public interface INotificationRepository : IRepository<Notification>
{
    /// <summary>
    /// Bulk-flags every unread notification for <paramref name="userId"/> as read via
    /// <c>ExecuteUpdateAsync</c>, so "mark all as read" is one statement instead of loading
    /// every row into the change tracker.
    /// </summary>
    Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);
}
