using PersonaNest.Domain.Abstractions;
using PersonaNest.Domain.Enums;
using PersonaNest.Services.Common;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.Interfaces;

/// <summary>Admin dashboard, user management, categories and the moderation queue (§23).</summary>
public interface IAdminService
{
    Task<AdminStatsDto> GetDashboardStatsAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<UserCardDto>> GetUsersAsync(
        string? query, int page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> PromoteAsync(
        string userId, string role, CancellationToken cancellationToken = default);

    Task<ServiceResult> DemoteAsync(
        string userId, string role, CancellationToken cancellationToken = default);

    /// <summary>Ban enforcement uses Identity lockout; the reason is stored for display (D-9).</summary>
    Task<ServiceResult> BanAsync(
        BanUserRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult> UnbanAsync(
        string userId, CancellationToken cancellationToken = default);

    // ── Category management (§15)
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    Task<ServiceResult<int>> SaveCategoryAsync(
        SaveCategoryRequest request, CancellationToken cancellationToken = default);

    /// <summary>Fails while media still reference the category - the FK is Restrict.</summary>
    Task<ServiceResult> DeleteCategoryAsync(
        int categoryId, CancellationToken cancellationToken = default);

    // ── Moderation queue (Specification v3 §6)
    Task<PagedResult<ReportQueueItemDto>> GetReportQueueAsync(
        ReportStatus? status = null, ReportTargetType? targetType = null,
        int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}
