using PersonaNest.Domain.Abstractions;
using PersonaNest.Domain.Entities;
using PersonaNest.Domain.Enums;
using PersonaNest.Services.Common;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.DTOs.Responses;
using PersonaNest.Services.Interfaces;
using PersonaNest.Services.Mapping;

namespace PersonaNest.Services.Implementations;

/// <inheritdoc cref="IMediaService"/>
public class MediaService : IMediaService
{
    private readonly IUnitOfWork _uow;

    public MediaService(IUnitOfWork uow)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
    }

    public async Task<PagedResult<MediaCardDto>> SearchAsync(
        MediaSearchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var items = await _uow.Media.SearchAsync(
            request.Query, request.CategoryId, MediaMappings.ToCardDto,
            request.Page, request.PageSize, cancellationToken);

        var total = await _uow.Media.CountSearchAsync(
            request.Query, request.CategoryId, cancellationToken);

        return new PagedResult<MediaCardDto>(items, total, request.Page, request.PageSize);
    }

    public Task<MediaDetailDto?> GetDetailsAsync(
        int mediaId, string? viewerId, CancellationToken cancellationToken = default)
        => _uow.Media.FirstOrDefaultAsync(
            m => m.Id == mediaId,
            MediaMappings.ToDetailDto(viewerId),
            cancellationToken);

    public Task<IReadOnlyList<MediaCardDto>> GetTrendingAsync(
        int count = 10, CancellationToken cancellationToken = default)
        => _uow.Media.ListAsync(
            m => m.Status != MediaStatus.Rejected,
            MediaMappings.ToCardDto,
            q => q.OrderByDescending(m => m.EntryCount).ThenByDescending(m => m.AverageRating),
            page: 1, pageSize: count, cancellationToken);

    public Task<IReadOnlyList<MediaCardDto>> FindPossibleDuplicatesAsync(
        string title, int categoryId, int? releaseYear,
        CancellationToken cancellationToken = default)
        => _uow.Media.FindPossibleDuplicatesAsync(
            title, categoryId, releaseYear, MediaMappings.ToCardDto, cancellationToken);

    public async Task<ServiceResult<int>> CreateAsync(
        CreateMediaRequest request, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var categoryExists = await _uow.Repository<Category>()
            .AnyAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (!categoryExists)
        {
            return ServiceResult<int>.Failure("That category does not exist.");
        }

        // §4: warn about duplicates, but let the user proceed once they have seen the list.
        if (!request.ConfirmedNotDuplicate)
        {
            var duplicates = await FindPossibleDuplicatesAsync(
                request.Title, request.CategoryId, request.ReleaseYear, cancellationToken);

            if (duplicates.Count > 0)
            {
                return ServiceResult<int>.Failure(
                    "This may already be in the database. Review the matches, then confirm to add it anyway.");
            }
        }

        var media = request.ToEntity(userId, DateTime.UtcNow);

        await _uow.Media.AddAsync(media, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return ServiceResult<int>.Success(media.Id);
    }

    public async Task<ServiceResult> UpdateAsync(
        UpdateMediaRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var media = await _uow.Media.GetByIdAsync(request.Id, cancellationToken);
        if (media is null)
        {
            return ServiceResult.Failure("That media item no longer exists.");
        }

        var categoryExists = await _uow.Repository<Category>()
            .AnyAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (!categoryExists)
        {
            return ServiceResult.Failure("That category does not exist.");
        }

        request.ApplyTo(media, DateTime.UtcNow);
        _uow.Media.Update(media);
        await _uow.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SoftDeleteAsync(
        int mediaId, string moderatorId, CancellationToken cancellationToken = default)
    {
        var media = await _uow.Media.GetByIdAsync(mediaId, cancellationToken);
        if (media is null)
        {
            return ServiceResult.Failure("That media item no longer exists.");
        }

        // Soft delete only. Media -> Entry is Restrict, so a hard delete would fail anyway -
        // and that constraint exists precisely to protect other users' entries (decision D-9).
        media.IsDeleted = true;
        media.DeletedAt = DateTime.UtcNow;
        media.DeletedById = moderatorId;
        media.Status = MediaStatus.Rejected;

        _uow.Media.Update(media);
        await _uow.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
        => _uow.Repository<Category>().ListAsync(
            filter: null,
            LookupMappings.ToCategoryDto,
            q => q.OrderBy(c => c.Name),
            page: 1, pageSize: 50, cancellationToken);
}
