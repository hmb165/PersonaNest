namespace PersonaNest.Domain.Abstractions;

/// <summary>
/// Coordinates repository work and owns persistence (§9). Services take a dependency on this,
/// never on a DbContext (§8, rule 4).
/// <para>
/// One instance per request. <see cref="SaveChangesAsync"/> commits every change made through
/// every repository obtained from this unit of work in a single transaction, so an operation
/// that touches several aggregates - creating an Entry and recomputing its Media's cached
/// aggregates, for example - either lands completely or not at all.
/// </para>
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    IEntryRepository Entries { get; }
    IMediaRepository Media { get; }
    IReportRepository Reports { get; }

    /// <summary>
    /// The generic repository for any entity without a specific one. Instances are cached per
    /// unit of work, so repeated calls share change tracking.
    /// </summary>
    IRepository<T> Repository<T>() where T : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>True when an explicit transaction is currently open.</summary>
    bool HasActiveTransaction { get; }

    /// <summary>
    /// Begins an explicit transaction. Only needed to span several
    /// <see cref="SaveChangesAsync"/> calls - a single SaveChangesAsync is already atomic.
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
