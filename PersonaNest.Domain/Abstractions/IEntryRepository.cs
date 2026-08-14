using System.Linq.Expressions;
using PersonaNest.Domain.Entities;

namespace PersonaNest.Domain.Abstractions;

/// <summary>
/// Entry queries too shaped to express through <see cref="IRepository{T}"/> alone - chiefly the
/// privacy rules of §18, which must be evaluated in SQL rather than in memory.
/// </summary>
public interface IEntryRepository : IRepository<Entry>
{
    /// <summary>
    /// Has this user already logged this media? Backs the unique (UserId, MediaId) rule
    /// (decision D-11).
    /// </summary>
    Task<bool> ExistsForUserAndMediaAsync(
        string userId, int mediaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The existing entry's id, or null. Lets the create flow redirect to
    /// <c>/Entries/Edit/{id}</c> instead of failing on the unique index.
    /// </summary>
    Task<int?> FindIdForUserAndMediaAsync(
        string userId, int mediaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// A profile's entries as a given viewer is allowed to see them.
    /// Public to everyone; FollowersOnly to followers and the owner; Private to the owner only.
    /// Pass <paramref name="viewerId"/> null for an anonymous visitor.
    /// </summary>
    Task<IReadOnlyList<TResult>> GetVisibleForProfileAsync<TResult>(
        string profileUserId,
        string? viewerId,
        Expression<Func<Entry, TResult>> selector,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<int> CountVisibleForProfileAsync(
        string profileUserId, string? viewerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// One entry, projected, but only if <paramref name="viewerId"/> is allowed to see it -
    /// returns default when the §18 rule excludes them. Keeps the privacy decision in SQL and
    /// out of the service layer.
    /// </summary>
    Task<TResult?> GetVisibleByIdAsync<TResult>(
        int entryId,
        string? viewerId,
        Expression<Func<Entry, TResult>> selector,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Community entries on a media page, filtered to what <paramref name="viewerId"/> may see.
    /// Same §18 rule as the profile query; added in Phase 4 because the media page needs it and
    /// the visibility expression must stay inside Infrastructure.
    /// </summary>
    Task<IReadOnlyList<TResult>> GetVisibleForMediaAsync<TResult>(
        int mediaId,
        string? viewerId,
        Expression<Func<Entry, TResult>> selector,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<int> CountVisibleForMediaAsync(
        int mediaId, string? viewerId, CancellationToken cancellationToken = default);

    /// <summary>Recent entries from the people a user follows - the dashboard activity feed.</summary>
    Task<IReadOnlyList<TResult>> GetFollowingFeedAsync<TResult>(
        string viewerId,
        Expression<Func<Entry, TResult>> selector,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}
