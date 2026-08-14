using PersonaNest.Domain.Enums;

namespace PersonaNest.Services.DTOs.Responses;

/// <summary>A moderator application as the applicant and the reviewing admin see it.</summary>
public sealed record ModeratorApplicationDto
{
    public int Id { get; init; }

    public string UserId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? ProfilePictureUrl { get; init; }

    /// <summary>Shown on the review card as "91 entries".</summary>
    public int ApplicantEntryCount { get; init; }
    public DateTime ApplicantJoinedAt { get; init; }

    public string Reason { get; init; } = string.Empty;
    public string? RelevantExperience { get; init; }

    public ApplicationStatus Status { get; init; }
    public DateTime AppliedAt { get; init; }

    public string? ReviewedByAdminUserName { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? AdminNotes { get; init; }
}

/// <summary>
/// One row of the unified moderation queue.
/// <para>
/// Mapped from the Domain read model <c>ReportQueueItem</c>, which the report repository builds
/// by merging three separate tables (decision D-4). <see cref="TargetType"/> supplies the
/// <c>{targetType}</c> segment of
/// <c>POST /Moderation/Reports/{targetType}/Resolve/{id}</c> - an id alone is ambiguous across
/// three tables.
/// </para>
/// </summary>
public sealed record ReportQueueItemDto
{
    public int Id { get; init; }
    public ReportTargetType TargetType { get; init; }
    public int TargetId { get; init; }
    public string TargetLabel { get; init; } = string.Empty;
    public string? TargetSnippet { get; init; }
    public string ReporterUserName { get; init; } = string.Empty;
    public ReportReason Reason { get; init; }
    public ReportStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>System-level counters for the admin dashboard (§23).</summary>
public sealed record AdminStatsDto
{
    public int TotalUsers { get; init; }
    public int TotalMedia { get; init; }
    public int TotalEntries { get; init; }
    public int TotalCollections { get; init; }
    public int PendingApplications { get; init; }
    public int OpenReports { get; init; }
    public int MediaAwaitingReview { get; init; }
    public int UsersJoinedThisWeek { get; init; }
    public int EntriesThisWeek { get; init; }
}
