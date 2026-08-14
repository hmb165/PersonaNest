using System.Linq.Expressions;

namespace PersonaNest.Domain.Abstractions;

/// <summary>
/// The generic repository contract (§8).
/// <para>
/// Note the shape of <see cref="ListAsync{TResult}"/>: the caller passes the <em>projection</em>
/// in rather than receiving entities back. That is what lets §8 (no DbContext outside
/// repositories) and §13 (AsNoTracking, projection, pagination, no over-fetching) hold at the
/// same time. A conventional <c>Task&lt;IEnumerable&lt;T&gt;&gt; GetAllAsync()</c> would make
/// §13 impossible, because once rows are materialised you can no longer project or page in SQL.
/// </para>
/// <para>
/// No <see cref="IQueryable{T}"/> ever leaves the Infrastructure layer, so a query can never be
/// composed - or accidentally enumerated - after the Unit of Work has been disposed.
/// </para>
/// </summary>
public interface IRepository<T> where T : class
{
    /// <summary>Tracked single-entity fetch by primary key. Use when you intend to modify it.</summary>
    Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracked fetch for an entity with a <b>composite</b> primary key - CollectionItem
    /// (CollectionId, MediaId) and EntryTag (EntryId, TagId). Key values must be given in the
    /// order the key is declared.
    /// </summary>
    Task<T?> GetByKeysAsync(object[] keyValues, CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Projected, untracked, paged read. The projection is translated into the SQL SELECT list
    /// and paging is applied by the database.
    /// </summary>
    Task<IReadOnlyList<TResult>> ListAsync<TResult>(
        Expression<Func<T, bool>>? filter,
        Expression<Func<T, TResult>> selector,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Projected, untracked single-item read - for detail pages that need a ViewModel rather
    /// than a tracked entity.
    /// </summary>
    Task<TResult?> FirstOrDefaultAsync<TResult>(
        Expression<Func<T, bool>> filter,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default);

    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    void Update(T entity);

    void Remove(T entity);

    void RemoveRange(IEnumerable<T> entities);
}
