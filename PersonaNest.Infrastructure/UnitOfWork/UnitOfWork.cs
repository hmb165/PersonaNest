using Microsoft.EntityFrameworkCore.Storage;
using PersonaNest.Domain.Abstractions;
using PersonaNest.Infrastructure.Data;
using PersonaNest.Infrastructure.Repositories;

namespace PersonaNest.Infrastructure.UnitOfWork;

/// <inheritdoc cref="IUnitOfWork"/>
public class UnitOfWork : IUnitOfWork
{
    private readonly PersonaNestDbContext _context;
    private readonly Dictionary<Type, object> _repositories = new();

    private IEntryRepository? _entries;
    private IMediaRepository? _media;
    private IReportRepository? _reports;
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    public UnitOfWork(PersonaNestDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IEntryRepository Entries => _entries ??= new EntryRepository(_context);

    public IMediaRepository Media => _media ??= new MediaRepository(_context);

    public IReportRepository Reports => _reports ??= new ReportRepository(_context);

    /// <summary>
    /// Cached per unit of work, so two calls for the same entity share one change tracker view.
    /// </summary>
    public IRepository<T> Repository<T>() where T : class
    {
        if (_repositories.TryGetValue(typeof(T), out var existing))
        {
            return (IRepository<T>)existing;
        }

        var created = new Repository<T>(_context);
        _repositories[typeof(T)] = created;
        return created;
    }

    /// <summary>
    /// Commits every pending change. A single call is already atomic - EF wraps it in an
    /// implicit transaction - so an explicit transaction is only needed to span several calls.
    /// </summary>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public bool HasActiveTransaction => _transaction is not null;

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException("A transaction is already in progress.");
        }

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("There is no transaction to commit.");
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.RollbackAsync(cancellationToken);
        await DisposeTransactionAsync();
    }

    /// <summary>
    /// Disposes an abandoned transaction only. The DbContext is owned by the DI container
    /// (registered with AddDbContext, scoped to the request) and must not be disposed here.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await DisposeTransactionAsync();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private async Task DisposeTransactionAsync()
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.DisposeAsync();
        _transaction = null;
    }
}
