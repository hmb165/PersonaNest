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
    private readonly ITasteProfileCalculator _tasteProfileCalculator;

    public ProfileService(IUnitOfWork uow, ITasteProfileCalculator tasteProfileCalculator)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        _tasteProfileCalculator = tasteProfileCalculator
            ?? throw new ArgumentNullException(nameof(tasteProfileCalculator));
    }

    public Task<ProfileHeaderDto?> GetByUserNameAsync(
        string userName, string? viewerId, CancellationToken cancellationToken = default)
        => _uow.Repository<ApplicationUser>().FirstOrDefaultAsync(
            u => u.UserName == userName && !u.IsDeleted,
            UserMappings.ToProfileHeaderDto(viewerId),
            cancellationToken);

    public Task<AppearanceDto?> GetAppearanceAsync(
        string userId, CancellationToken cancellationToken = default)
        => _uow.Repository<ApplicationUser>().FirstOrDefaultAsync(
            u => u.Id == userId && !u.IsDeleted,
            UserMappings.ToAppearanceDto,
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

        // A single server-side AVG() (§13) - the previous version paged the raw ratings into
        // memory to average them in C#, which silently capped a prolific user's average at their
        // first 100 rated entries instead of covering all of them (Phase 13 finding).
        var averageRating = await _uow.Entries.GetAverageRatingAsync(userId, cancellationToken);

        return new ProfileStatsDto
        {
            EntryCount = entryCount,
            ReviewCount = reviewCount,
            FollowerCount = followerCount,
            FollowingCount = followingCount,
            CollectionCount = collectionCount,
            FavoriteCount = favoriteCount,
            AverageRating = averageRating is { } avg
                ? Math.Round(avg, 1, MidpointRounding.AwayFromZero)
                : null
        };
    }

    public async Task<TasteProfileDto?> GetTasteProfileAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var persisted = await _uow.Repository<TasteProfile>().FirstOrDefaultAsync(
            tp => tp.UserId == userId, TasteProfileMappings.ToDto, cancellationToken);

        if (persisted is not null)
        {
            return persisted;
        }

        // The Phase 12 background service hasn't computed a row for this user yet (or never
        // will, for a user with few entries). Rather than show an empty state, compute the same
        // shape on demand from real Entry/Tag data (§22), via the same calculator the background
        // service uses. Deliberately not persisted here - Phase 12 owns writing the TasteProfile
        // table; this is a read-time fallback, not a second competing model.
        return await _tasteProfileCalculator.ComputeAsync(userId, cancellationToken);
    }

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
