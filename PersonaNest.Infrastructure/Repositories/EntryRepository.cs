using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PersonaNest.Domain.Abstractions;
using PersonaNest.Domain.Entities;
using PersonaNest.Infrastructure.Data;

namespace PersonaNest.Infrastructure.Repositories;

/// <inheritdoc cref="IEntryRepository"/>
public class EntryRepository : Repository<Entry>, IEntryRepository
{
    public EntryRepository(PersonaNestDbContext context) : base(context)
    {
    }

    public Task<bool> ExistsForUserAndMediaAsync(
        string userId, int mediaId, CancellationToken cancellationToken = default)
        => AnyAsync(e => e.UserId == userId && e.MediaId == mediaId, cancellationToken);

    public async Task<int?> FindIdForUserAndMediaAsync(
        string userId, int mediaId, CancellationToken cancellationToken = default)
    {
        // Projects to int? so a miss returns null without materialising the entity.
        return await Set.AsNoTracking()
            .Where(e => e.UserId == userId && e.MediaId == mediaId)
            .Select(e => (int?)e.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TResult>> GetVisibleForProfileAsync<TResult>(
        string profileUserId,
        string? viewerId,
        Expression<Func<Entry, TResult>> selector,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var (skip, take) = Paging(page, pageSize);

        return await Set.AsNoTracking()
            .Where(e => e.UserId == profileUserId)
            .Where(EntryVisibility.For(viewerId))
            .OrderByDescending(e => e.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(selector)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountVisibleForProfileAsync(
        string profileUserId, string? viewerId, CancellationToken cancellationToken = default)
        => Set.AsNoTracking()
              .Where(e => e.UserId == profileUserId)
              .Where(EntryVisibility.For(viewerId))
              .CountAsync(cancellationToken);

    public async Task<TResult?> GetVisibleByIdAsync<TResult>(
        int entryId,
        string? viewerId,
        Expression<Func<Entry, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return await Set.AsNoTracking()
            .Where(e => e.Id == entryId)
            .Where(EntryVisibility.For(viewerId))
            .Select(selector)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TResult>> GetVisibleForMediaAsync<TResult>(
        int mediaId,
        string? viewerId,
        Expression<Func<Entry, TResult>> selector,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var (skip, take) = Paging(page, pageSize);

        return await Set.AsNoTracking()
            .Where(e => e.MediaId == mediaId)
            .Where(EntryVisibility.For(viewerId))
            .OrderByDescending(e => e.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(selector)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountVisibleForMediaAsync(
        int mediaId, string? viewerId, CancellationToken cancellationToken = default)
        => Set.AsNoTracking()
              .Where(e => e.MediaId == mediaId)
              .Where(EntryVisibility.For(viewerId))
              .CountAsync(cancellationToken);

    public async Task<IReadOnlyList<TResult>> GetFollowingFeedAsync<TResult>(
        string viewerId,
        Expression<Func<Entry, TResult>> selector,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var (skip, take) = Paging(page, pageSize);

        // "Authored by someone I follow" plus the same visibility rule, both evaluated in SQL.
        return await Set.AsNoTracking()
            .Where(e => e.User.Followers.Any(f => f.FollowerId == viewerId))
            .Where(EntryVisibility.For(viewerId))
            .OrderByDescending(e => e.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(selector)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal?> GetAverageRatingAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var rated = Set.AsNoTracking().Where(e => e.UserId == userId && e.Rating != null);

        // AVG() over an empty set is SQL NULL either way, but this avoids the round trip and
        // reads clearer than checking the nullable result.
        return await rated.AnyAsync(cancellationToken)
            ? await rated.AverageAsync(e => e.Rating!.Value, cancellationToken)
            : null;
    }
}
