using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PersonaNest.Domain.Abstractions;
using PersonaNest.Infrastructure.Data;

namespace PersonaNest.Infrastructure.Repositories;

/// <summary>
/// Generic repository. Every read path is <c>AsNoTracking</c> and projected, so §13 holds by
/// construction rather than by discipline.
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    /// <summary>Upper bound on page size, so a hostile or buggy caller cannot ask for the table.</summary>
    public const int MaxPageSize = 100;

    protected readonly PersonaNestDbContext Context;
    protected readonly DbSet<T> Set;

    public Repository(PersonaNestDbContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Set = context.Set<T>();
    }

    /// <summary>Tracked - the caller intends to modify and then save.</summary>
    public virtual async Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        => await Set.FindAsync(new[] { id }, cancellationToken);

    public virtual async Task<T?> GetByKeysAsync(
        object[] keyValues, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyValues);
        return await Set.FindAsync(keyValues, cancellationToken);
    }

    public virtual Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
        => Set.AsNoTracking().AnyAsync(predicate, cancellationToken);

    public virtual Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
        => predicate is null
            ? Set.AsNoTracking().CountAsync(cancellationToken)
            : Set.AsNoTracking().CountAsync(predicate, cancellationToken);

    public virtual async Task<IReadOnlyList<TResult>> ListAsync<TResult>(
        Expression<Func<T, bool>>? filter,
        Expression<Func<T, TResult>> selector,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selector);

        IQueryable<T> query = Set.AsNoTracking();

        if (filter is not null)
        {
            query = query.Where(filter);
        }

        if (orderBy is not null)
        {
            query = orderBy(query);
        }

        var (skip, take) = Paging(page, pageSize);

        // Select last: EF translates the projection into the SELECT list, so only the needed
        // columns cross the wire.
        return await query
            .Skip(skip)
            .Take(take)
            .Select(selector)
            .ToListAsync(cancellationToken);
    }

    public virtual async Task<TResult?> FirstOrDefaultAsync<TResult>(
        Expression<Func<T, bool>> filter,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(selector);

        return await Set.AsNoTracking()
            .Where(filter)
            .Select(selector)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await Set.AddAsync(entity, cancellationToken);
    }

    public virtual async Task AddRangeAsync(
        IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);
        await Set.AddRangeAsync(entities, cancellationToken);
    }

    public virtual void Update(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Set.Update(entity);
    }

    public virtual void Remove(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Set.Remove(entity);
    }

    public virtual void RemoveRange(IEnumerable<T> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        Set.RemoveRange(entities);
    }

    /// <summary>Normalises page arguments so callers cannot page past the guard rails.</summary>
    protected static (int Skip, int Take) Paging(int page, int pageSize)
    {
        if (page < 1)
        {
            page = 1;
        }

        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        return ((page - 1) * pageSize, pageSize);
    }
}
