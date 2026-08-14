using Microsoft.AspNetCore.Identity;
using PersonaNest.Domain.Abstractions;
using PersonaNest.Domain.Constants;
using PersonaNest.Domain.Entities;
using PersonaNest.Domain.Enums;
using PersonaNest.Services.Common;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.DTOs.Responses;
using PersonaNest.Services.Interfaces;
using PersonaNest.Services.Mapping;

namespace PersonaNest.Services.Implementations;

/// <inheritdoc cref="IAdminService"/>
/// <remarks>
/// Uses <see cref="UserManager{TUser}"/> for role membership and lockout only - both live in
/// Identity's tables and have no safe repository equivalent. All other data access goes through
/// the Unit of Work. Flagged in the Phase 4 report.
/// </remarks>
public class AdminService : IAdminService
{
    private readonly IUnitOfWork _uow;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminService(IUnitOfWork uow, UserManager<ApplicationUser> userManager)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    public async Task<AdminStatsDto> GetDashboardStatsAsync(
        CancellationToken cancellationToken = default)
    {
        var weekAgo = DateTime.UtcNow.AddDays(-7);

        return new AdminStatsDto
        {
            TotalUsers = await _uow.Repository<ApplicationUser>()
                .CountAsync(u => !u.IsDeleted, cancellationToken),
            TotalMedia = await _uow.Media.CountAsync(null, cancellationToken),
            TotalEntries = await _uow.Entries.CountAsync(null, cancellationToken),
            TotalCollections = await _uow.Repository<Collection>()
                .CountAsync(null, cancellationToken),
            PendingApplications = await _uow.Repository<ModeratorApplication>()
                .CountAsync(a => a.Status == ApplicationStatus.Pending, cancellationToken),
            OpenReports = await _uow.Reports.CountOpenAsync(cancellationToken),
            MediaAwaitingReview = await _uow.Reports
                .CountMediaAwaitingReviewAsync(cancellationToken),
            UsersJoinedThisWeek = await _uow.Repository<ApplicationUser>()
                .CountAsync(u => u.CreatedAt >= weekAgo, cancellationToken),
            EntriesThisWeek = await _uow.Entries
                .CountAsync(e => e.CreatedAt >= weekAgo, cancellationToken)
        };
    }

    public async Task<PagedResult<UserCardDto>> GetUsersAsync(
        string? query, int page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var repository = _uow.Repository<ApplicationUser>();
        var needle = string.IsNullOrWhiteSpace(query) ? null : query.Trim();

        var items = await repository.ListAsync(
            u => needle == null
                 || u.UserName!.Contains(needle)
                 || u.DisplayName.Contains(needle)
                 || (u.Email != null && u.Email.Contains(needle)),
            UserMappings.ToAdminCardDto,
            q => q.OrderByDescending(u => u.CreatedAt),
            page, pageSize, cancellationToken);

        var total = await repository.CountAsync(
            u => needle == null
                 || u.UserName!.Contains(needle)
                 || u.DisplayName.Contains(needle)
                 || (u.Email != null && u.Email.Contains(needle)),
            cancellationToken);

        // Role membership lives in Identity's tables, so it is filled in after projection.
        var withRoles = new List<UserCardDto>(items.Count);
        foreach (var item in items)
        {
            var user = await _userManager.FindByIdAsync(item.Id);
            var roles = user is null
                ? Array.Empty<string>()
                : (await _userManager.GetRolesAsync(user)).ToArray();

            withRoles.Add(item with { Roles = roles });
        }

        return new PagedResult<UserCardDto>(withRoles, total, page, pageSize);
    }

    public Task<ServiceResult> PromoteAsync(
        string userId, string role, CancellationToken cancellationToken = default)
        => ChangeRoleAsync(userId, role, add: true);

    public Task<ServiceResult> DemoteAsync(
        string userId, string role, CancellationToken cancellationToken = default)
        => ChangeRoleAsync(userId, role, add: false);

    public async Task<ServiceResult> BanAsync(
        BanUserRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            return ServiceResult.Failure("That account no longer exists.");
        }

        if (await _userManager.IsInRoleAsync(user, Roles.Admin))
        {
            return ServiceResult.Failure("Administrators cannot be banned.");
        }

        // Enforcement is Identity lockout - no parallel ban flag to keep in sync (decision D-9).
        var until = request.BannedUntil.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(request.BannedUntil.Value, DateTimeKind.Utc))
            : DateTimeOffset.MaxValue;

        await _userManager.SetLockoutEnabledAsync(user, true);
        var lockout = await _userManager.SetLockoutEndDateAsync(user, until);

        if (!lockout.Succeeded)
        {
            return ServiceResult.Failure(lockout.Errors.Select(e => e.Description).ToArray());
        }

        // BanReason is display-only, and lives on our own column.
        var repository = _uow.Repository<ApplicationUser>();
        var tracked = await repository.GetByIdAsync(request.UserId, cancellationToken);
        if (tracked is not null)
        {
            tracked.BanReason = request.Reason.Trim();
            repository.Update(tracked);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UnbanAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult.Failure("That account no longer exists.");
        }

        var lockout = await _userManager.SetLockoutEndDateAsync(user, null);
        if (!lockout.Succeeded)
        {
            return ServiceResult.Failure(lockout.Errors.Select(e => e.Description).ToArray());
        }

        var repository = _uow.Repository<ApplicationUser>();
        var tracked = await repository.GetByIdAsync(userId, cancellationToken);
        if (tracked is not null)
        {
            tracked.BanReason = null;
            repository.Update(tracked);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        return ServiceResult.Success();
    }

    public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
        => _uow.Repository<Category>().ListAsync(
            filter: null, LookupMappings.ToCategoryDto,
            q => q.OrderBy(c => c.Name), page: 1, pageSize: 50, cancellationToken);

    public async Task<ServiceResult<int>> SaveCategoryAsync(
        SaveCategoryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var repository = _uow.Repository<Category>();
        var name = request.Name.Trim();
        var slug = request.Slug.Trim().ToLowerInvariant();

        // Mirrors the unique indexes on Name and Slug.
        var clash = await repository.AnyAsync(
            c => c.Id != (request.Id ?? 0) && (c.Name == name || c.Slug == slug),
            cancellationToken);

        if (clash)
        {
            return ServiceResult<int>.Failure(
                "Another category already uses that name or slug.");
        }

        Category category;
        if (request.Id is { } id)
        {
            var existing = await repository.GetByIdAsync(id, cancellationToken);
            if (existing is null)
            {
                return ServiceResult<int>.Failure("That category no longer exists.");
            }

            category = existing;
        }
        else
        {
            category = new Category();
            await repository.AddAsync(category, cancellationToken);
        }

        category.Name = name;
        category.Slug = slug;
        category.Description = string.IsNullOrWhiteSpace(request.Description)
            ? null : request.Description.Trim();
        category.Icon = string.IsNullOrWhiteSpace(request.Icon) ? null : request.Icon.Trim();
        category.ColorToken = request.ColorToken.Trim();

        if (request.Id is not null)
        {
            repository.Update(category);
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return ServiceResult<int>.Success(category.Id);
    }

    public async Task<ServiceResult> DeleteCategoryAsync(
        int categoryId, CancellationToken cancellationToken = default)
    {
        var repository = _uow.Repository<Category>();
        var category = await repository.GetByIdAsync(categoryId, cancellationToken);

        if (category is null)
        {
            return ServiceResult.Failure("That category no longer exists.");
        }

        // Category -> Media is Restrict, so this would throw at the database. Checking first
        // turns it into a message the admin screen can show.
        var inUse = await _uow.Media.AnyAsync(m => m.CategoryId == categoryId, cancellationToken);
        if (inUse)
        {
            return ServiceResult.Failure(
                "This category still has media. Reassign or remove that media first.");
        }

        repository.Remove(category);
        await _uow.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    public async Task<PagedResult<ReportQueueItemDto>> GetReportQueueAsync(
        ReportStatus? status = null, ReportTargetType? targetType = null,
        int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var queue = await _uow.Reports.GetQueueAsync(
            status, targetType, page, pageSize, cancellationToken);

        return new PagedResult<ReportQueueItemDto>(
            queue.Items.Select(ModerationMappings.ToQueueItemDto).ToList(),
            queue.TotalCount,
            queue.Page,
            queue.PageSize);
    }

    private async Task<ServiceResult> ChangeRoleAsync(string userId, string role, bool add)
    {
        if (!Roles.All.Contains(role))
        {
            return ServiceResult.Failure($"'{role}' is not a valid role.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult.Failure("That account no longer exists.");
        }

        var inRole = await _userManager.IsInRoleAsync(user, role);
        if (add == inRole)
        {
            return ServiceResult.Success();
        }

        var result = add
            ? await _userManager.AddToRoleAsync(user, role)
            : await _userManager.RemoveFromRoleAsync(user, role);

        return result.Succeeded
            ? ServiceResult.Success()
            : ServiceResult.Failure(result.Errors.Select(e => e.Description).ToArray());
    }
}
