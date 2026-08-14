using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.ViewModels;

/// <summary>/Moderation/Apply (§7).</summary>
public sealed class ModeratorApplicationViewModel
{
    public SubmitModeratorApplicationRequest Form { get; set; } = new();

    /// <summary>Shown instead of the form when an application is already pending.</summary>
    public ModeratorApplicationDto? ExistingApplication { get; set; }

    public bool CanApply => ExistingApplication is null
        || ExistingApplication.Status != Domain.Enums.ApplicationStatus.Pending;
}

/// <summary>/Moderation/Dashboard — queue counters plus recent flagged content.</summary>
public sealed class ModeratorDashboardViewModel
{
    public int OpenReports { get; set; }
    public int MediaAwaitingReview { get; set; }
    public int ResolvedLast30Days { get; set; }
    public IReadOnlyList<ReportQueueItemDto> RecentReports { get; set; }
        = Array.Empty<ReportQueueItemDto>();
}

/// <summary>/Moderation/Reports — the merged, filterable queue.</summary>
public sealed class ReportQueueViewModel
{
    public Domain.Enums.ReportStatus? Status { get; set; }
    public Domain.Enums.ReportTargetType? TargetType { get; set; }
    public IReadOnlyList<ReportQueueItemDto> Items { get; set; }
        = Array.Empty<ReportQueueItemDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>/Admin — system overview (§23).</summary>
public sealed class AdminDashboardViewModel
{
    public AdminStatsDto Stats { get; set; } = new();
    public IReadOnlyList<UserCardDto> RecentUsers { get; set; } = Array.Empty<UserCardDto>();
    public IReadOnlyList<ModeratorApplicationDto> PendingApplications { get; set; }
        = Array.Empty<ModeratorApplicationDto>();
}

/// <summary>/Admin/Users.</summary>
public sealed class UserManagementViewModel
{
    public string? Query { get; set; }
    public IReadOnlyList<UserCardDto> Users { get; set; } = Array.Empty<UserCardDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>/Admin/Categories (§15).</summary>
public sealed class CategoryManagementViewModel
{
    public IReadOnlyList<CategoryDto> Categories { get; set; } = Array.Empty<CategoryDto>();
    public SaveCategoryRequest Form { get; set; } = new();
    public bool IsEdit => Form.Id is not null;
}

/// <summary>/Admin/Applications.</summary>
public sealed class ModeratorApplicationsViewModel
{
    public IReadOnlyList<ModeratorApplicationDto> Applications { get; set; }
        = Array.Empty<ModeratorApplicationDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
}
