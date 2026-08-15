using System.Linq.Expressions;
using PersonaNest.Domain.Entities;

namespace PersonaNest.Domain.Abstractions;

/// <summary>
/// Collection queries too shaped to express through <see cref="IRepository{T}"/> alone - chiefly
/// that <c>CollectionItem</c>-&gt;<c>Media</c> is a <em>required</em> navigation into an entity
/// with a global soft-delete query filter (<c>MediaConfiguration.HasQueryFilter</c>). Projecting
/// through it with filters active turns into an inner join that silently drops the whole
/// <c>CollectionItem</c> row - and therefore the collection's item count - the moment its media is
/// soft-deleted, exactly what <c>CollectionItemConfiguration</c>'s <c>Restrict</c> delete
/// behaviour was chosen to prevent (Phase 13 finding).
/// </summary>
public interface ICollectionRepository : IRepository<Collection>
{
    /// <summary>
    /// One collection, with its items visible even if their media was later soft-deleted.
    /// </summary>
    Task<TResult?> GetDetailsIncludingRemovedMediaAsync<TResult>(
        Expression<Func<Collection, bool>> filter,
        Expression<Func<Collection, TResult>> selector,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// A page of collections, with each one's item count/preview unaffected by soft-deleted
    /// media in the same way as <see cref="GetDetailsIncludingRemovedMediaAsync{TResult}"/>.
    /// </summary>
    Task<IReadOnlyList<TResult>> ListIncludingRemovedMediaAsync<TResult>(
        Expression<Func<Collection, bool>> filter,
        Expression<Func<Collection, TResult>> selector,
        Func<IQueryable<Collection>, IOrderedQueryable<Collection>> orderBy,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
