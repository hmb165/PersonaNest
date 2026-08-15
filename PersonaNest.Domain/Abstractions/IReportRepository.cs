using PersonaNest.Domain.Enums;

namespace PersonaNest.Domain.Abstractions;

/// <summary>
/// The moderation queue across <c>MediaReport</c>, <c>EntryReport</c> and
/// <c>CommentReport</c> (Specification v3 §6).
/// </summary>
public interface IReportRepository
{
    /// <summary>
    /// One page of the merged queue, newest first.
    /// <para>
    /// Implemented as three projected untracked queries each taking <c>skip + take</c>, merged
    /// and re-sorted in memory, plus three COUNTs for the total - bounded work, never a full
    /// table scan (§13). Filtering by <paramref name="targetType"/> short-circuits to a single
    /// query.
    /// </para>
    /// </summary>
    Task<PagedResult<ReportQueueItem>> GetQueueAsync(
        ReportStatus? status = null,
        ReportTargetType? targetType = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>Open reports across all three tables - the "Pending Reports" dashboard stat.</summary>
    Task<int> CountOpenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Distinct media with at least one open report - the "Media to Review" stat. Media has no
    /// pending-approval state (new media defaults to Approved, decision D-9), so the review
    /// queue is defined by open reports.
    /// </summary>
    Task<int> CountMediaAwaitingReviewAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reports resolved (not dismissed) on or after <paramref name="since"/>, across all three
    /// tables - the Moderator Dashboard's "Resolved (30d)" stat.
    /// </summary>
    Task<int> CountResolvedSinceAsync(DateTime since, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks one still-<see cref="ReportStatus.Open"/> report <paramref name="newStatus"/>, in
    /// whichever of the three tables <paramref name="targetType"/> indicates - the id alone is
    /// ambiguous across them (decision D-4). Mutates the tracked entity only; the caller commits
    /// through <see cref="IUnitOfWork.SaveChangesAsync"/>, matching every other write in this
    /// codebase. Returns false when no matching <em>open</em> report exists (already handled, or
    /// never existed), so the service can turn that into a friendly message instead of silently
    /// overwriting a previous resolution.
    /// </summary>
    Task<bool> ResolveAsync(
        ReportTargetType targetType, int reportId, string moderatorId, ReportStatus newStatus,
        string? resolutionNotes, CancellationToken cancellationToken = default);
}
