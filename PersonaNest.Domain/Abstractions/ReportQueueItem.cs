using PersonaNest.Domain.Enums;

namespace PersonaNest.Domain.Abstractions;

/// <summary>
/// One row of the unified moderation queue, flattened from whichever of the three report tables
/// it came from.
/// <para>
/// Reports stay as three tables with real foreign keys (decision D-4); this read model is how
/// the design's single queue is assembled. <see cref="TargetType"/> supplies the
/// <c>{targetType}</c> segment for
/// <c>POST /Moderation/Reports/{targetType}/Resolve/{id}</c>, since an id alone is ambiguous
/// across three tables.
/// </para>
/// <para>
/// A Domain-level read model rather than a Services DTO, because Domain cannot reference
/// Services. Phase 4 maps it to <c>ReportQueueItemDto</c> using Manual Mapping.
/// </para>
/// </summary>
public sealed record ReportQueueItem(
    int Id,
    ReportTargetType TargetType,
    int TargetId,
    string TargetLabel,
    string? TargetSnippet,
    string ReporterId,
    string ReporterUserName,
    ReportReason Reason,
    ReportStatus Status,
    DateTime CreatedAt);
