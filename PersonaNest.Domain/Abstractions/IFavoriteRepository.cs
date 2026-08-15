using System.Linq.Expressions;
using PersonaNest.Domain.Entities;

namespace PersonaNest.Domain.Abstractions;

/// <summary>
/// Favorite queries too shaped to express through <see cref="IRepository{T}"/> alone - the same
/// required-navigation-through-a-soft-delete-filter issue as <see cref="ICollectionRepository"/>:
/// <c>Favorite</c>-&gt;<c>Media</c> is required and <c>Restrict</c> on delete specifically so
/// favoriting survives the media being removed later, but projecting through it with filters
/// active silently drops the row anyway (Phase 13 finding).
/// </summary>
public interface IFavoriteRepository : IRepository<Favorite>
{
    /// <summary>A user's favorites, with entries visible even if their media was later soft-deleted.</summary>
    Task<IReadOnlyList<TResult>> ListIncludingRemovedMediaAsync<TResult>(
        Expression<Func<Favorite, bool>> filter,
        Expression<Func<Favorite, TResult>> selector,
        Func<IQueryable<Favorite>, IOrderedQueryable<Favorite>> orderBy,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
