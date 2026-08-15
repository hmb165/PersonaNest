using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PersonaNest.Domain.Abstractions;
using PersonaNest.Domain.Entities;
using PersonaNest.Infrastructure.Data;

namespace PersonaNest.Infrastructure.Repositories;

/// <inheritdoc cref="ICollectionRepository"/>
public class CollectionRepository : Repository<Collection>, ICollectionRepository
{
    public CollectionRepository(PersonaNestDbContext context) : base(context)
    {
    }

    public async Task<TResult?> GetDetailsIncludingRemovedMediaAsync<TResult>(
        Expression<Func<Collection, bool>> filter,
        Expression<Func<Collection, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(selector);

        // IgnoreQueryFilters so CollectionItem->Media - a required navigation into a soft-deleted
        // entity - doesn't silently inner-join the item out of existence. Media.Status stays
        // Rejected/soft-deleted either way; this only affects whether an owner's own saved item
        // still renders, matching why that relationship is Restrict and not Cascade.
        return await Set.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(filter)
            .Select(selector)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TResult>> ListIncludingRemovedMediaAsync<TResult>(
        Expression<Func<Collection, bool>> filter,
        Expression<Func<Collection, TResult>> selector,
        Func<IQueryable<Collection>, IOrderedQueryable<Collection>> orderBy,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(orderBy);

        var (skip, take) = Paging(page, pageSize);

        return await orderBy(Set.IgnoreQueryFilters().AsNoTracking().Where(filter))
            .Skip(skip)
            .Take(take)
            .Select(selector)
            .ToListAsync(cancellationToken);
    }
}
