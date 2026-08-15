using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PersonaNest.Domain.Abstractions;
using PersonaNest.Domain.Entities;
using PersonaNest.Services.Interfaces;

namespace PersonaNest.Services.BackgroundServices;

/// <summary>
/// The project's one background service, running the two documented tasks from §10/D-20:
/// <list type="number">
/// <item>Taste-profile recomputation - refreshes every user's persisted <see cref="TasteProfile"/>
/// (§22), so <see cref="IProfileService.GetTasteProfileAsync"/> can read a fresh row instead of
/// falling back to on-demand computation.</item>
/// <item>Media aggregate reconciliation - re-derives every <see cref="Media"/> row's
/// <c>AverageRating</c>/<c>RatingCount</c>/<c>EntryCount</c> from scratch. This is the safety net:
/// the primary path is the synchronous recompute already wired into
/// <c>EntryService.CreateAsync</c>/<c>UpdateAsync</c>/<c>DeleteAsync</c>, which keeps the cached
/// columns correct on every write. This nightly pass only corrects drift.</item>
/// </list>
/// Both tasks run as independent loops on their own configurable interval, concurrently, inside
/// this single <see cref="BackgroundService"/> - not as two separately-registered hosted
/// services. Each cycle opens its own DI scope so neither task holds a scoped
/// <see cref="IUnitOfWork"/>/DbContext open for the service's whole lifetime, and a failure in
/// one task (or one item within a task) never stops the other.
/// </summary>
public sealed class PersonaNestBackgroundService : BackgroundService
{
    private const int PageSize = 100;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PersonaNestBackgroundService> _logger;
    private readonly TimeSpan _tasteProfileInterval;
    private readonly TimeSpan _reconciliationInterval;

    public PersonaNestBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<PersonaNestBackgroundService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Approved default kept as-is (Phase 12 report, 2026-08-15) - do not change without
        // re-approval.
        var tasteProfileMinutes = configuration.GetValue<int?>("TasteProfile:RefreshIntervalMinutes") ?? 15;
        _tasteProfileInterval = TimeSpan.FromMinutes(Math.Max(1, tasteProfileMinutes));

        // Spec §10 calls this task "nightly" - 24 hours is the spec-conformant default.
        var reconciliationHours = configuration.GetValue<int?>("MediaAggregates:ReconciliationIntervalHours") ?? 24;
        _reconciliationInterval = TimeSpan.FromHours(Math.Max(1, reconciliationHours));
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "PersonaNest background service starting. Taste-profile interval: {TasteProfileInterval}; " +
            "aggregate-reconciliation interval: {ReconciliationInterval}.",
            _tasteProfileInterval, _reconciliationInterval);

        return Task.WhenAll(
            RunLoopAsync(_tasteProfileInterval, RefreshAllTasteProfilesAsync, stoppingToken),
            RunLoopAsync(_reconciliationInterval, ReconcileAllMediaAggregatesAsync, stoppingToken));
    }

    /// <summary>Runs <paramref name="runOnceAsync"/> immediately, then again every <paramref name="interval"/>.</summary>
    private static async Task RunLoopAsync(
        TimeSpan interval, Func<CancellationToken, Task> runOnceAsync, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await runOnceAsync(stoppingToken);

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    // ── Task 1: taste-profile refresh ────────────────────────────────────────────────────

    private async Task RefreshAllTasteProfilesAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var calculator = scope.ServiceProvider.GetRequiredService<ITasteProfileCalculator>();

        var userIds = new List<string>();
        var page = 1;
        while (true)
        {
            IReadOnlyList<string> batch;
            try
            {
                batch = await uow.Repository<ApplicationUser>().ListAsync(
                    filter: u => !u.IsDeleted,
                    selector: u => u.Id,
                    orderBy: q => q.OrderBy(u => u.Id),
                    page: page,
                    pageSize: PageSize,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Taste profile refresh: failed to list users, aborting this cycle.");
                return;
            }

            userIds.AddRange(batch);
            if (batch.Count < PageSize)
            {
                break;
            }

            page++;
        }

        _logger.LogInformation("Taste profile refresh: starting for {Count} users.", userIds.Count);

        var refreshed = 0;
        var failed = 0;
        foreach (var userId in userIds)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                if (await calculator.RefreshAsync(userId, cancellationToken))
                {
                    refreshed++;

                    // Bonus: AI. Best-effort - a narrative failure (no API key, rate limit, etc.)
                    // must never count as a failed taste-profile refresh, since the stats above
                    // already persisted successfully.
                    try
                    {
                        await calculator.RefreshNarrativeAsync(userId, cancellationToken);
                    }
                    catch (Exception narrativeEx)
                    {
                        _logger.LogWarning(
                            narrativeEx, "AI narrative refresh failed for user {UserId}.", userId);
                    }
                }
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogWarning(ex, "Taste profile refresh failed for user {UserId}.", userId);
            }
        }

        _logger.LogInformation(
            "Taste profile refresh: completed. {Refreshed} updated, {Failed} failed, {Total} users checked.",
            refreshed, failed, userIds.Count);
    }

    // ── Task 2: media aggregate reconciliation ───────────────────────────────────────────

    private async Task ReconcileAllMediaAggregatesAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var mediaIds = new List<int>();
        var page = 1;
        while (true)
        {
            IReadOnlyList<int> batch;
            try
            {
                batch = await uow.Repository<Media>().ListAsync(
                    filter: null,
                    selector: m => m.Id,
                    orderBy: q => q.OrderBy(m => m.Id),
                    page: page,
                    pageSize: PageSize,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, "Media aggregate reconciliation: failed to list media, aborting this cycle.");
                return;
            }

            mediaIds.AddRange(batch);
            if (batch.Count < PageSize)
            {
                break;
            }

            page++;
        }

        _logger.LogInformation(
            "Media aggregate reconciliation: starting for {Count} media items.", mediaIds.Count);

        var reconciled = 0;
        var failed = 0;

        await uow.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var mediaId in mediaIds)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    // Public entries only (D-16) - RecalculateAggregatesAsync mirrors the exact
                    // rule the synchronous write-through already applies in EntryService, so the
                    // nightly pass can only ever agree with it, never introduce a second rule.
                    await uow.Media.RecalculateAggregatesAsync(mediaId, cancellationToken);
                    reconciled++;
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogWarning(
                        ex, "Media aggregate reconciliation failed for media {MediaId}.", mediaId);
                }
            }

            await uow.SaveChangesAsync(cancellationToken);
            await uow.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await uow.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Media aggregate reconciliation: batch save failed, rolled back.");
            return;
        }

        _logger.LogInformation(
            "Media aggregate reconciliation: completed. {Reconciled} reconciled, {Failed} failed, " +
            "{Total} media items checked.",
            reconciled, failed, mediaIds.Count);
    }
}
