using Microsoft.EntityFrameworkCore;
using PersonaNest.Domain.Abstractions;
using PersonaNest.Domain.Enums;
using PersonaNest.Infrastructure.Data;

namespace PersonaNest.Infrastructure.Repositories;

/// <inheritdoc cref="IReportRepository"/>
/// <remarks>
/// Reports live in three tables with real foreign keys (decision D-4), so the single queue the
/// design shows is assembled here rather than by the database. Every query uses
/// <c>IgnoreQueryFilters</c>: moderators must still see reports whose target has already been
/// soft-deleted, otherwise resolved-by-removal cases would vanish from the audit trail.
/// </remarks>
public class ReportRepository : IReportRepository
{
    public const int MaxPageSize = 100;

    private readonly PersonaNestDbContext _context;

    public ReportRepository(PersonaNestDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PagedResult<ReportQueueItem>> GetQueueAsync(
        ReportStatus? status = null,
        ReportTargetType? targetType = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            page = 1;
        }

        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        // Each source query takes skip + take. Bounded work: at most 3 * (skip + take) rows are
        // read, never a full table (§13).
        var ceiling = ((page - 1) * pageSize) + pageSize;

        var wantMedia = targetType is null or ReportTargetType.Media;
        var wantEntry = targetType is null or ReportTargetType.Entry;
        var wantComment = targetType is null or ReportTargetType.Comment;

        var buckets = new List<IReadOnlyList<ReportQueueItem>>(3);
        var total = 0;

        if (wantMedia)
        {
            var q = _context.MediaReports.IgnoreQueryFilters().AsNoTracking();
            if (status.HasValue)
            {
                q = q.Where(r => r.Status == status.Value);
            }

            total += await q.CountAsync(cancellationToken);
            buckets.Add(await q
                .OrderByDescending(r => r.CreatedAt)
                .Take(ceiling)
                .Select(r => new ReportQueueItem(
                    r.Id,
                    ReportTargetType.Media,
                    r.MediaId,
                    r.Media.Title,
                    r.Media.Creator,
                    r.ReporterId,
                    r.Reporter.UserName!,
                    r.Reason,
                    r.Status,
                    r.CreatedAt))
                .ToListAsync(cancellationToken));
        }

        if (wantEntry)
        {
            var q = _context.EntryReports.IgnoreQueryFilters().AsNoTracking();
            if (status.HasValue)
            {
                q = q.Where(r => r.Status == status.Value);
            }

            total += await q.CountAsync(cancellationToken);
            buckets.Add(await q
                .OrderByDescending(r => r.CreatedAt)
                .Take(ceiling)
                .Select(r => new ReportQueueItem(
                    r.Id,
                    ReportTargetType.Entry,
                    r.EntryId,
                    "Entry by @" + r.Entry.User.UserName,
                    r.Entry.Review,
                    r.ReporterId,
                    r.Reporter.UserName!,
                    r.Reason,
                    r.Status,
                    r.CreatedAt))
                .ToListAsync(cancellationToken));
        }

        if (wantComment)
        {
            var q = _context.CommentReports.IgnoreQueryFilters().AsNoTracking();
            if (status.HasValue)
            {
                q = q.Where(r => r.Status == status.Value);
            }

            total += await q.CountAsync(cancellationToken);
            buckets.Add(await q
                .OrderByDescending(r => r.CreatedAt)
                .Take(ceiling)
                .Select(r => new ReportQueueItem(
                    r.Id,
                    ReportTargetType.Comment,
                    r.CommentId,
                    "Comment by @" + r.Comment.User.UserName,
                    r.Comment.Content,
                    r.ReporterId,
                    r.Reporter.UserName!,
                    r.Reason,
                    r.Status,
                    r.CreatedAt))
                .ToListAsync(cancellationToken));
        }

        // Merge, re-sort, then take the requested page.
        var items = buckets
            .SelectMany(b => b)
            .OrderByDescending(i => i.CreatedAt)
            .ThenByDescending(i => i.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<ReportQueueItem>(items, total, page, pageSize);
    }

    public async Task<int> CountOpenAsync(CancellationToken cancellationToken = default)
    {
        var media = await _context.MediaReports.IgnoreQueryFilters()
            .CountAsync(r => r.Status == ReportStatus.Open, cancellationToken);
        var entry = await _context.EntryReports.IgnoreQueryFilters()
            .CountAsync(r => r.Status == ReportStatus.Open, cancellationToken);
        var comment = await _context.CommentReports.IgnoreQueryFilters()
            .CountAsync(r => r.Status == ReportStatus.Open, cancellationToken);

        return media + entry + comment;
    }

    public Task<int> CountMediaAwaitingReviewAsync(CancellationToken cancellationToken = default)
        => _context.MediaReports
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(r => r.Status == ReportStatus.Open)
            .Select(r => r.MediaId)
            .Distinct()
            .CountAsync(cancellationToken);
}
