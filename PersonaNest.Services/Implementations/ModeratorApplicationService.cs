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

/// <inheritdoc cref="IModeratorApplicationService"/>
/// <remarks>
/// Takes a dependency on <see cref="UserManager{TUser}"/> for one reason only: approving an
/// application must assign the Moderator Identity role, and role membership lives in Identity's
/// own tables. Writing AspNetUserRoles by hand through a repository would bypass Identity's
/// normalisation and security stamp. This is flagged in the Phase 4 report.
/// </remarks>
public class ModeratorApplicationService : IModeratorApplicationService
{
    private readonly IUnitOfWork _uow;
    private readonly UserManager<ApplicationUser> _userManager;

    public ModeratorApplicationService(IUnitOfWork uow, UserManager<ApplicationUser> userManager)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    public async Task<ServiceResult<int>> SubmitAsync(
        string userId, SubmitModeratorApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var repository = _uow.Repository<ModeratorApplication>();

        // Mirrors the filtered unique index UX_ModeratorApplication_User_Pending.
        var pending = await repository.AnyAsync(
            a => a.UserId == userId && a.Status == ApplicationStatus.Pending, cancellationToken);

        if (pending)
        {
            return ServiceResult<int>.Failure(
                "You already have an application awaiting review.");
        }

        if (await _uow.Repository<ApplicationUser>()
                      .AnyAsync(u => u.Id == userId && u.IsDeleted, cancellationToken))
        {
            return ServiceResult<int>.Failure("That account is no longer active.");
        }

        var application = new ModeratorApplication
        {
            UserId = userId,
            Reason = request.Reason.Trim(),
            RelevantExperience = string.IsNullOrWhiteSpace(request.RelevantExperience)
                ? null
                : request.RelevantExperience.Trim(),
            Status = ApplicationStatus.Pending,
            AppliedAt = DateTime.UtcNow
        };

        await repository.AddAsync(application, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return ServiceResult<int>.Success(application.Id);
    }

    public async Task<ModeratorApplicationDto?> GetLatestForUserAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var results = await _uow.Repository<ModeratorApplication>().ListAsync(
            a => a.UserId == userId,
            ModerationMappings.ToApplicationDto,
            q => q.OrderByDescending(a => a.AppliedAt),
            page: 1, pageSize: 1, cancellationToken);

        return results.FirstOrDefault();
    }

    public async Task<PagedResult<ModeratorApplicationDto>> GetPendingAsync(
        int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var repository = _uow.Repository<ModeratorApplication>();

        var items = await repository.ListAsync(
            a => a.Status == ApplicationStatus.Pending,
            ModerationMappings.ToApplicationDto,
            q => q.OrderBy(a => a.AppliedAt),
            page, pageSize, cancellationToken);

        var total = await repository.CountAsync(
            a => a.Status == ApplicationStatus.Pending, cancellationToken);

        return new PagedResult<ModeratorApplicationDto>(items, total, page, pageSize);
    }

    public Task<ModeratorApplicationDto?> GetByIdAsync(
        int applicationId, CancellationToken cancellationToken = default)
        => _uow.Repository<ModeratorApplication>().FirstOrDefaultAsync(
            a => a.Id == applicationId, ModerationMappings.ToApplicationDto, cancellationToken);

    public async Task<ServiceResult> ReviewAsync(
        ReviewModeratorApplicationRequest request, string adminId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var repository = _uow.Repository<ModeratorApplication>();
        var application = await repository.GetByIdAsync(request.ApplicationId, cancellationToken);

        if (application is null)
        {
            return ServiceResult.Failure("That application no longer exists.");
        }

        if (application.Status != ApplicationStatus.Pending)
        {
            return ServiceResult.Failure("That application has already been reviewed.");
        }

        // AdminNotes maps to a nvarchar(2000) column (ModeratorApplicationConfiguration); an
        // over-length value would otherwise fail at SaveChanges with a raw DB error instead of a
        // friendly ServiceResult (§12).
        if (request.AdminNotes?.Length > 2000)
        {
            return ServiceResult.Failure("Notes cannot exceed 2000 characters.");
        }

        application.Status = request.Approve
            ? ApplicationStatus.Approved
            : ApplicationStatus.Rejected;
        application.ReviewedByAdminId = adminId;
        application.ReviewedAt = DateTime.UtcNow;
        application.AdminNotes = string.IsNullOrWhiteSpace(request.AdminNotes)
            ? null
            : request.AdminNotes.Trim();

        repository.Update(application);

        // Approval must actually grant the role, otherwise the §7 workflow has no effect.
        if (request.Approve)
        {
            var user = await _userManager.FindByIdAsync(application.UserId);
            if (user is null)
            {
                return ServiceResult.Failure("The applicant's account no longer exists.");
            }

            if (!await _userManager.IsInRoleAsync(user, Roles.Moderator))
            {
                var assigned = await _userManager.AddToRoleAsync(user, Roles.Moderator);
                if (!assigned.Succeeded)
                {
                    return ServiceResult.Failure(
                        assigned.Errors.Select(e => e.Description).ToArray());
                }
            }
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }
}
