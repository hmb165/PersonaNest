using PersonaNest.Domain.Abstractions;
using PersonaNest.Domain.Entities;
using PersonaNest.Services.Common;
using PersonaNest.Services.DTOs.Responses;
using PersonaNest.Services.Interfaces;

namespace PersonaNest.Services.Implementations;

/// <inheritdoc cref="IFavoriteService"/>
public class FavoriteService : IFavoriteService
{
    private readonly IUnitOfWork _uow;

    public FavoriteService(IUnitOfWork uow)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
    }

    public async Task<PagedResult<MediaCardDto>> GetForUserAsync(
        string userId, int page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var repository = _uow.Repository<Favorite>();

        // Projected straight from Favorite through its Media navigation, so no second query and
        // no over-fetching (§13).
        var items = await repository.ListAsync(
            f => f.UserId == userId,
            f => new MediaCardDto
            {
                Id = f.Media.Id,
                Title = f.Media.Title,
                OfficialCoverUrl = f.Media.OfficialCoverUrl,
                Creator = f.Media.Creator,
                ReleaseYear = f.Media.ReleaseYear,
                CategoryName = f.Media.Category.Name,
                CategoryColorToken = f.Media.Category.ColorToken,
                AverageRating = f.Media.AverageRating,
                EntryCount = f.Media.EntryCount
            },
            q => q.OrderByDescending(f => f.CreatedAt),
            page, pageSize, cancellationToken);

        var total = await repository.CountAsync(f => f.UserId == userId, cancellationToken);

        return new PagedResult<MediaCardDto>(items, total, page, pageSize);
    }

    public Task<bool> IsFavoritedAsync(
        string userId, int mediaId, CancellationToken cancellationToken = default)
        => _uow.Repository<Favorite>()
               .AnyAsync(f => f.UserId == userId && f.MediaId == mediaId, cancellationToken);

    public async Task<ServiceResult<bool>> ToggleAsync(
        string userId, int mediaId, CancellationToken cancellationToken = default)
    {
        if (!await _uow.Media.AnyAsync(m => m.Id == mediaId, cancellationToken))
        {
            return ServiceResult<bool>.Failure("That media item no longer exists.");
        }

        var repository = _uow.Repository<Favorite>();

        var existingId = await repository.FirstOrDefaultAsync(
            f => f.UserId == userId && f.MediaId == mediaId,
            f => (int?)f.Id,
            cancellationToken);

        if (existingId is { } id)
        {
            var tracked = await repository.GetByIdAsync(id, cancellationToken);
            if (tracked is not null)
            {
                repository.Remove(tracked);
                await _uow.SaveChangesAsync(cancellationToken);
            }

            return ServiceResult<bool>.Success(false);
        }

        await repository.AddAsync(new Favorite
        {
            UserId = userId,
            MediaId = mediaId,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }
}
