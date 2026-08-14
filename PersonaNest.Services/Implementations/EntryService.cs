using PersonaNest.Domain.Abstractions;
using PersonaNest.Domain.Entities;
using PersonaNest.Services.Common;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.DTOs.Responses;
using PersonaNest.Services.Interfaces;
using PersonaNest.Services.Mapping;

namespace PersonaNest.Services.Implementations;

/// <inheritdoc cref="IEntryService"/>
public class EntryService : IEntryService
{
    private readonly IUnitOfWork _uow;

    public EntryService(IUnitOfWork uow)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
    }

    public async Task<PagedResult<EntryCardDto>> GetForProfileAsync(
        string profileUserId, string? viewerId,
        int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var items = await _uow.Entries.GetVisibleForProfileAsync(
            profileUserId, viewerId, EntryMappings.ToCardDto, page, pageSize, cancellationToken);

        var total = await _uow.Entries.CountVisibleForProfileAsync(
            profileUserId, viewerId, cancellationToken);

        return new PagedResult<EntryCardDto>(items, total, page, pageSize);
    }

    public async Task<PagedResult<EntrySummaryDto>> GetMineAsync(
        string userId, MyEntriesRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // The owner sees everything of their own, so no visibility filter is needed here.
        var items = await _uow.Entries.ListAsync(
            e => e.UserId == userId
                 && (request.CategoryId == null || e.Media.CategoryId == request.CategoryId)
                 && (request.Status == null || e.Status == request.Status),
            EntryMappings.ToSummaryDto,
            q => q.OrderByDescending(e => e.CreatedAt),
            request.Page, request.PageSize, cancellationToken);

        var total = await _uow.Entries.CountAsync(
            e => e.UserId == userId
                 && (request.CategoryId == null || e.Media.CategoryId == request.CategoryId)
                 && (request.Status == null || e.Status == request.Status),
            cancellationToken);

        return new PagedResult<EntrySummaryDto>(items, total, request.Page, request.PageSize);
    }

    public Task<EntryDetailDto?> GetDetailsAsync(
        int entryId, string? viewerId, CancellationToken cancellationToken = default)
        => _uow.Entries.GetVisibleByIdAsync(
            entryId, viewerId, EntryMappings.ToDetailDto(viewerId), cancellationToken);

    public async Task<PagedResult<EntryCardDto>> GetForMediaAsync(
        int mediaId, string? viewerId,
        int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var items = await _uow.Entries.GetVisibleForMediaAsync(
            mediaId, viewerId, EntryMappings.ToCardDto, page, pageSize, cancellationToken);

        var total = await _uow.Entries.CountVisibleForMediaAsync(
            mediaId, viewerId, cancellationToken);

        return new PagedResult<EntryCardDto>(items, total, page, pageSize);
    }

    public Task<IReadOnlyList<EntryCardDto>> GetFollowingFeedAsync(
        string viewerId, int page = 1, int pageSize = 10,
        CancellationToken cancellationToken = default)
        => _uow.Entries.GetFollowingFeedAsync(
            viewerId, EntryMappings.ToCardDto, page, pageSize, cancellationToken);

    public Task<int?> FindExistingEntryIdAsync(
        string userId, int mediaId, CancellationToken cancellationToken = default)
        => _uow.Entries.FindIdForUserAndMediaAsync(userId, mediaId, cancellationToken);

    public async Task<ServiceResult<int>> CreateAsync(
        CreateEntryRequest request, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var mediaExists = await _uow.Media.AnyAsync(m => m.Id == request.MediaId, cancellationToken);
        if (!mediaExists)
        {
            return ServiceResult<int>.Failure("That media item no longer exists.");
        }

        // Unique (UserId, MediaId): one entry per user per media, no rewatch rows (D-11).
        if (await _uow.Entries.ExistsForUserAndMediaAsync(userId, request.MediaId, cancellationToken))
        {
            return ServiceResult<int>.Failure(
                "You have already logged this media. Edit your existing entry instead.");
        }

        if (!IsValidRating(request.Rating))
        {
            return ServiceResult<int>.Failure(
                "Rating must be between 0.5 and 10.0, in steps of 0.5.");
        }

        var entry = request.ToEntity(userId, DateTime.UtcNow);

        // Two saves - the entry must exist before the aggregate recount can see it - so they are
        // wrapped in one transaction and land together.
        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            await _uow.Entries.AddAsync(entry, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            await SyncTagsAsync(entry.Id, request.TagIds, cancellationToken);
            await _uow.Media.RecalculateAggregatesAsync(entry.MediaId, cancellationToken);

            await _uow.CommitAsync(cancellationToken);
        }
        catch
        {
            await _uow.RollbackAsync(cancellationToken);
            throw;
        }

        return ServiceResult<int>.Success(entry.Id);
    }

    public async Task<ServiceResult> UpdateAsync(
        UpdateEntryRequest request, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entry = await _uow.Entries.GetByIdAsync(request.Id, cancellationToken);
        if (entry is null)
        {
            return ServiceResult.Failure("That entry no longer exists.");
        }

        if (entry.UserId != userId)
        {
            return ServiceResult.Failure("You can only edit your own entries.");
        }

        if (!IsValidRating(request.Rating))
        {
            return ServiceResult.Failure("Rating must be between 0.5 and 10.0, in steps of 0.5.");
        }

        request.ApplyTo(entry, DateTime.UtcNow);
        _uow.Entries.Update(entry);

        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            await _uow.SaveChangesAsync(cancellationToken);
            await SyncTagsAsync(entry.Id, request.TagIds, cancellationToken);

            // Privacy or rating may have changed, so the public-only aggregates must be redone.
            await _uow.Media.RecalculateAggregatesAsync(entry.MediaId, cancellationToken);

            await _uow.CommitAsync(cancellationToken);
        }
        catch
        {
            await _uow.RollbackAsync(cancellationToken);
            throw;
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(
        int entryId, string userId, CancellationToken cancellationToken = default)
    {
        var entry = await _uow.Entries.GetByIdAsync(entryId, cancellationToken);
        if (entry is null)
        {
            return ServiceResult.Failure("That entry no longer exists.");
        }

        if (entry.UserId != userId)
        {
            return ServiceResult.Failure("You can only delete your own entries.");
        }

        entry.IsDeleted = true;
        entry.DeletedAt = DateTime.UtcNow;
        entry.DeletedById = userId;
        _uow.Entries.Update(entry);

        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            await _uow.SaveChangesAsync(cancellationToken);
            await _uow.Media.RecalculateAggregatesAsync(entry.MediaId, cancellationToken);
            await _uow.CommitAsync(cancellationToken);
        }
        catch
        {
            await _uow.RollbackAsync(cancellationToken);
            throw;
        }

        return ServiceResult.Success();
    }

    public Task<IReadOnlyList<TagDto>> GetTagsAsync(CancellationToken cancellationToken = default)
        => _uow.Repository<Tag>().ListAsync(
            filter: null, LookupMappings.ToTagDto,
            q => q.OrderBy(t => t.Name), page: 1, pageSize: 100, cancellationToken);

    /// <summary>
    /// Replaces an entry's tags with the requested set. Clear-and-re-add is correct here:
    /// EntryTag carries no payload, and the set is small.
    /// </summary>
    private async Task SyncTagsAsync(
        int entryId, IReadOnlyList<int> tagIds, CancellationToken cancellationToken)
    {
        var repository = _uow.Repository<EntryTag>();

        var existing = await repository.ListAsync(
            et => et.EntryId == entryId,
            et => new { et.EntryId, et.TagId },
            orderBy: null, page: 1, pageSize: 100, cancellationToken);

        var wanted = tagIds.Distinct().ToHashSet();
        var current = existing.Select(e => e.TagId).ToHashSet();

        var toRemove = current.Except(wanted)
            .Select(tagId => new EntryTag { EntryId = entryId, TagId = tagId })
            .ToList();

        var toAdd = wanted.Except(current)
            .Select(tagId => new EntryTag { EntryId = entryId, TagId = tagId })
            .ToList();

        if (toRemove.Count > 0)
        {
            repository.RemoveRange(toRemove);
        }

        if (toAdd.Count > 0)
        {
            await repository.AddRangeAsync(toAdd, cancellationToken);
        }

        if (toRemove.Count > 0 || toAdd.Count > 0)
        {
            await _uow.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Mirrors the CK_Entry_RatingRange and CK_Entry_RatingHalfStep check constraints, so an
    /// invalid value produces a friendly message instead of a database error (§12).
    /// </summary>
    private static bool IsValidRating(decimal? rating)
    {
        if (rating is null)
        {
            return true;
        }

        return rating >= 0.5m
            && rating <= 10.0m
            && rating.Value * 2 == Math.Floor(rating.Value * 2);
    }
}
