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
}
