using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PersonaNest.Domain.Abstractions;
using PersonaNest.Domain.Entities;
using PersonaNest.Domain.Enums;
using PersonaNest.Infrastructure.Data;

namespace PersonaNest.Infrastructure.Repositories;

/// <inheritdoc cref="IMediaRepository"/>
public class MediaRepository : Repository<Media>, IMediaRepository
{
    public MediaRepository(PersonaNestDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<TResult>> SearchAsync<TResult>(
        string? query,
        int? categoryId,
        Expression<Func<Media, TResult>> selector,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var (skip, take) = Paging(page, pageSize);

        return await BuildSearchQuery(query, categoryId)
            .OrderByDescending(m => m.EntryCount)
            .ThenBy(m => m.Title)
            .Skip(skip)
            .Take(take)
            .Select(selector)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountSearchAsync(
        string? query, int? categoryId, CancellationToken cancellationToken = default)
        => BuildSearchQuery(query, categoryId).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<TResult>> FindPossibleDuplicatesAsync<TResult>(
        string title,
        int categoryId,
        int? releaseYear,
        Expression<Func<Media, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (string.IsNullOrWhiteSpace(title))
        {
            return Array.Empty<TResult>();
        }

        var needle = title.Trim();

        var candidates = Set.AsNoTracking()
            .Where(m => m.CategoryId == categoryId && m.Title.Contains(needle));

        // A matching year is strong evidence; without one, title + category is the best signal
        // available. §4 asks to avoid duplicates "as much as reasonably possible", not to
        // guarantee it.
        if (releaseYear.HasValue)
        {
            candidates = candidates.Where(
                m => m.ReleaseYear == null || m.ReleaseYear == releaseYear.Value);
        }

        return await candidates
            .OrderBy(m => m.Title)
            .Take(10)
            .Select(selector)
            .ToListAsync(cancellationToken);
    }

    public async Task RecalculateAggregatesAsync(
        int mediaId, CancellationToken cancellationToken = default)
    {
        // Public entries only (decision D-16): the number a visitor sees must equal what a
        // visitor can actually reach. The global query filter already excludes soft-deleted
        // entries.
        var stats = await Context.Entries
            .AsNoTracking()
            .Where(e => e.MediaId == mediaId && e.Privacy == Privacy.Public)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                EntryCount = g.Count(),
                RatingCount = g.Count(e => e.Rating != null),
                Average = g.Average(e => (decimal?)e.Rating)
            })
            .FirstOrDefaultAsync(cancellationToken);

        // IgnoreQueryFilters: a soft-deleted media row still needs correct counts if it is
        // ever restored.
        var media = await Context.Media
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == mediaId, cancellationToken);

        if (media is null)
        {
            return;
        }

        media.EntryCount = stats?.EntryCount ?? 0;
        media.RatingCount = stats?.RatingCount ?? 0;
        media.AverageRating = stats?.Average is { } average
            ? Math.Round(average, 1, MidpointRounding.AwayFromZero)
            : null;

        // Deliberately no SaveChangesAsync - the Unit of Work commits, so this recount lands in
        // the same transaction as the entry change that triggered it (§9).
    }

    private IQueryable<Media> BuildSearchQuery(string? query, int? categoryId)
    {
        IQueryable<Media> media = Set.AsNoTracking();

        if (categoryId.HasValue)
        {
            media = media.Where(m => m.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var needle = query.Trim();
            media = media.Where(m =>
                m.Title.Contains(needle) ||
                (m.Creator != null && m.Creator.Contains(needle)));
        }

        // Rejected submissions never appear in search. Soft-deleted rows are already excluded
        // by the global query filter.
        return media.Where(m => m.Status != MediaStatus.Rejected);
    }
}
