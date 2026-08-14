using System.Linq.Expressions;
using PersonaNest.Domain.Entities;

namespace PersonaNest.Domain.Abstractions;

/// <summary>Catalogue search, duplicate detection, and the cached rating aggregates.</summary>
public interface IMediaRepository : IRepository<Media>
{
    /// <summary>Search by title or creator, optionally within one category (§4, §6).</summary>
    Task<IReadOnlyList<TResult>> SearchAsync<TResult>(
        string? query,
        int? categoryId,
        Expression<Func<Media, TResult>> selector,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<int> CountSearchAsync(
        string? query, int? categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Candidate duplicates for a proposed new Media row, so the Add Media flow can warn before
    /// inserting. §4: "avoid duplicate media as much as reasonably possible."
    /// </summary>
    Task<IReadOnlyList<TResult>> FindPossibleDuplicatesAsync<TResult>(
        string title,
        int categoryId,
        int? releaseYear,
        Expression<Func<Media, TResult>> selector,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Recomputes <c>AverageRating</c>, <c>RatingCount</c> and <c>EntryCount</c> from
    /// <b>public, non-deleted</b> entries only (decisions D-16 and D-20).
    /// <para>
    /// Mutates the tracked entity but does NOT save - the caller commits through
    /// <see cref="IUnitOfWork.SaveChangesAsync"/>, so the recount joins the same transaction as
    /// the entry change that triggered it.
    /// </para>
    /// </summary>
    Task RecalculateAggregatesAsync(int mediaId, CancellationToken cancellationToken = default);
}
