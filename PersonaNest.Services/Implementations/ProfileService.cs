using PersonaNest.Domain.Abstractions;
using PersonaNest.Domain.Entities;
using PersonaNest.Domain.Enums;
using PersonaNest.Services.Common;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.DTOs.Responses;
using PersonaNest.Services.Interfaces;
using PersonaNest.Services.Mapping;

namespace PersonaNest.Services.Implementations;

/// <inheritdoc cref="IProfileService"/>
public class ProfileService : IProfileService
{
    private readonly IUnitOfWork _uow;

    public ProfileService(IUnitOfWork uow)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
    }

    public Task<ProfileHeaderDto?> GetByUserNameAsync(
        string userName, string? viewerId, CancellationToken cancellationToken = default)
        => _uow.Repository<ApplicationUser>().FirstOrDefaultAsync(
            u => u.UserName == userName && !u.IsDeleted,
            UserMappings.ToProfileHeaderDto(viewerId),
            cancellationToken);

    public async Task<ProfileStatsDto> GetStatsAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        // Counted rather than projected in one shot, so each count is a cheap indexed query and
        // no cartesian join is produced across five collections (§13).
        var entryCount = await _uow.Entries.CountAsync(e => e.UserId == userId, cancellationToken);
        var reviewCount = await _uow.Entries.CountAsync(
            e => e.UserId == userId && e.Review != null && e.Review != "", cancellationToken);
        var followerCount = await _uow.Repository<Follow>()
            .CountAsync(f => f.FollowingId == userId, cancellationToken);
        var followingCount = await _uow.Repository<Follow>()
            .CountAsync(f => f.FollowerId == userId, cancellationToken);
        var collectionCount = await _uow.Repository<Collection>()
            .CountAsync(c => c.UserId == userId, cancellationToken);
        var favoriteCount = await _uow.Repository<Favorite>()
            .CountAsync(f => f.UserId == userId, cancellationToken);

        var ratings = await _uow.Entries.ListAsync(
            e => e.UserId == userId && e.Rating != null,
            e => e.Rating!.Value,
            orderBy: null, page: 1, pageSize: 100, cancellationToken);

        return new ProfileStatsDto
        {
            EntryCount = entryCount,
            ReviewCount = reviewCount,
            FollowerCount = followerCount,
            FollowingCount = followingCount,
            CollectionCount = collectionCount,
            FavoriteCount = favoriteCount,
            AverageRating = ratings.Count > 0
                ? Math.Round(ratings.Average(), 1, MidpointRounding.AwayFromZero)
                : null
        };
    }

    public Task<TasteProfileDto?> GetTasteProfileAsync(
        string userId, CancellationToken cancellationToken = default)
        => _uow.Repository<TasteProfile>().FirstOrDefaultAsync(
            tp => tp.UserId == userId, TasteProfileMappings.ToDto, cancellationToken);

    public async Task<ServiceResult> UpdateProfileAsync(
        string userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var repository = _uow.Repository<ApplicationUser>();
        var user = await repository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return ServiceResult.Failure("That account no longer exists.");
        }

        request.ApplyTo(user);
        repository.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UpdateAppearanceAsync(
        string userId, UpdateAppearanceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ThemeId is { } themeId)
        {
            var themeExists = await _uow.Repository<Theme>()
                .AnyAsync(t => t.Id == themeId, cancellationToken);

            if (!themeExists)
            {
                return ServiceResult.Failure("That theme does not exist.");
            }
        }

        var repository = _uow.Repository<ApplicationUser>();
        var user = await repository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return ServiceResult.Failure("That account no longer exists.");
        }

        request.ApplyTo(user);
        repository.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UpdatePrivacyAsync(
        string userId, UpdatePrivacyRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!Enum.IsDefined(typeof(Privacy), request.DefaultEntryPrivacy))
        {
            return ServiceResult.Failure("That privacy value is not valid.");
        }

        var repository = _uow.Repository<ApplicationUser>();
        var user = await repository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return ServiceResult.Failure("That account no longer exists.");
        }

        user.DefaultEntryPrivacy = request.DefaultEntryPrivacy;
        repository.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    public Task<IReadOnlyList<ThemeDto>> GetThemesAsync(CancellationToken cancellationToken = default)
        => _uow.Repository<Theme>().ListAsync(
            filter: null, LookupMappings.ToThemeDto,
            q => q.OrderByDescending(t => t.IsDefault).ThenBy(t => t.Name),
            page: 1, pageSize: 50, cancellationToken);
}
