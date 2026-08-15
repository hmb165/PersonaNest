using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PersonaNest.Domain.Abstractions;
using PersonaNest.Domain.Entities;
using PersonaNest.Infrastructure.Data;

namespace PersonaNest.Infrastructure.Repositories;

/// <inheritdoc cref="IFavoriteRepository"/>
public class FavoriteRepository : Repository<Favorite>, IFavoriteRepository
{
    public FavoriteRepository(PersonaNestDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<TResult>> ListIncludingRemovedMediaAsync<TResult>(
        Expression<Func<Favorite, bool>> filter,
        Expression<Func<Favorite, TResult>> selector,
        Func<IQueryable<Favorite>, IOrderedQueryable<Favorite>> orderBy,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(orderBy);

        var (skip, take) = Paging(page, pageSize);

        // IgnoreQueryFilters so Favorite->Media - required, Restrict specifically so removing
        // media doesn't wipe someone's favorites - doesn't silently inner-join the row away.
        return await orderBy(Set.IgnoreQueryFilters().AsNoTracking().Where(filter))
            .Skip(skip)
            .Take(take)
            .Select(selector)
            .ToListAsync(cancellationToken);
    }
}
