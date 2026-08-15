using PersonaNest.Domain.Abstractions;
using PersonaNest.Domain.Entities;
using PersonaNest.Domain.Enums;
using PersonaNest.Services.Common;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.DTOs.Responses;
using PersonaNest.Services.Interfaces;
using PersonaNest.Services.Mapping;

namespace PersonaNest.Services.Implementations;

/// <inheritdoc cref="ICollectionService"/>
public class CollectionService : ICollectionService
{
    private readonly IUnitOfWork _uow;

    public CollectionService(IUnitOfWork uow)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
    }

    public async Task<PagedResult<CollectionCardDto>> GetForUserAsync(
        string ownerId, string? viewerId,
        int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var isOwner = viewerId != null && viewerId == ownerId;

        // ICollectionRepository, not the generic repository: CollectionItem->Media is a required
        // navigation into a soft-delete-filtered entity, so a plain filtered query would silently
        // drop a collection's item (and undercount it) the moment that media is removed (§13).
        var items = await _uow.Collections.ListIncludingRemovedMediaAsync(
            c => c.UserId == ownerId && (isOwner || c.Privacy == Privacy.Public),
            CollectionMappings.ToCardDto,
            q => q.OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt),
            page, pageSize, cancellationToken);

        var total = await _uow.Collections.CountAsync(
            c => c.UserId == ownerId && (isOwner || c.Privacy == Privacy.Public),
            cancellationToken);

        return new PagedResult<CollectionCardDto>(items, total, page, pageSize);
    }

    public async Task<CollectionDetailDto?> GetDetailsAsync(
        int collectionId, string? viewerId, CancellationToken cancellationToken = default)
    {
        var collection = await _uow.Collections.GetDetailsIncludingRemovedMediaAsync(
            c => c.Id == collectionId,
            CollectionMappings.ToDetailDto(viewerId),
            cancellationToken);

        if (collection is null)
        {
            return null;
        }

        // Collections carry their own Privacy (§20). FollowersOnly resolves through the same
        // follow relationship as entries.
        return collection.Privacy switch
        {
            Privacy.Public => collection,
            Privacy.Private => collection.ViewerIsOwner ? collection : null,
            Privacy.FollowersOnly => collection.ViewerIsOwner
                || (viewerId != null && await FollowsAsync(viewerId, collection.OwnerId, cancellationToken))
                    ? collection
                    : null,
            _ => null
        };
    }

    public async Task<ServiceResult<int>> CreateAsync(
        CreateCollectionRequest request, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Data annotations don't reject an out-of-range enum bound from a raw int (§12).
        if (!Enum.IsDefined(typeof(Privacy), request.Privacy))
        {
            return ServiceResult<int>.Failure("That privacy value is not valid.");
        }

        var collection = request.ToEntity(userId, DateTime.UtcNow);

        await _uow.Repository<Collection>().AddAsync(collection, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return ServiceResult<int>.Success(collection.Id);
    }

    public async Task<ServiceResult> UpdateAsync(
        UpdateCollectionRequest request, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var repository = _uow.Repository<Collection>();
        var collection = await repository.GetByIdAsync(request.Id, cancellationToken);

        if (collection is null)
        {
            return ServiceResult.Failure("That collection no longer exists.");
        }

        if (collection.UserId != userId)
        {
            return ServiceResult.Failure("You can only edit your own collections.");
        }

        if (!Enum.IsDefined(typeof(Privacy), request.Privacy))
        {
            return ServiceResult.Failure("That privacy value is not valid.");
        }

        request.ApplyTo(collection, DateTime.UtcNow);
        repository.Update(collection);
        await _uow.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(
        int collectionId, string userId, CancellationToken cancellationToken = default)
    {
        var repository = _uow.Repository<Collection>();
        var collection = await repository.GetByIdAsync(collectionId, cancellationToken);

        if (collection is null)
        {
            return ServiceResult.Failure("That collection no longer exists.");
        }

        if (collection.UserId != userId)
        {
            return ServiceResult.Failure("You can only delete your own collections.");
        }

        // A real delete is safe here: Collection -> CollectionItem is Cascade, and a collection
        // is private to its owner rather than shared content.
        repository.Remove(collection);
        await _uow.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> AddItemAsync(
        AddCollectionItemRequest request, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var collection = await _uow.Repository<Collection>()
            .GetByIdAsync(request.CollectionId, cancellationToken);

        if (collection is null)
        {
            return ServiceResult.Failure("That collection no longer exists.");
        }

        if (collection.UserId != userId)
        {
            return ServiceResult.Failure("You can only change your own collections.");
        }

        if (!await _uow.Media.AnyAsync(m => m.Id == request.MediaId, cancellationToken))
        {
            return ServiceResult.Failure("That media item no longer exists.");
        }

        var items = _uow.Repository<CollectionItem>();

        var alreadyThere = await items.AnyAsync(
            i => i.CollectionId == request.CollectionId && i.MediaId == request.MediaId,
            cancellationToken);

        if (alreadyThere)
        {
            return ServiceResult.Failure("That item is already in this collection.");
        }

        var count = await items.CountAsync(
            i => i.CollectionId == request.CollectionId, cancellationToken);

        await items.AddAsync(new CollectionItem
        {
            CollectionId = request.CollectionId,
            MediaId = request.MediaId,
            AddedAt = DateTime.UtcNow,
            Position = count
        }, cancellationToken);

        collection.UpdatedAt = DateTime.UtcNow;
        _uow.Repository<Collection>().Update(collection);

        await _uow.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> RemoveItemAsync(
        int collectionId, int mediaId, string userId, CancellationToken cancellationToken = default)
    {
        var collection = await _uow.Repository<Collection>()
            .GetByIdAsync(collectionId, cancellationToken);

        if (collection is null)
        {
            return ServiceResult.Failure("That collection no longer exists.");
        }

        if (collection.UserId != userId)
        {
            return ServiceResult.Failure("You can only change your own collections.");
        }

        var items = _uow.Repository<CollectionItem>();
        var item = await items.GetByKeysAsync(new object[] { collectionId, mediaId }, cancellationToken);

        if (item is null)
        {
            return ServiceResult.Failure("That item is not in this collection.");
        }

        items.Remove(item);
        collection.UpdatedAt = DateTime.UtcNow;
        _uow.Repository<Collection>().Update(collection);

        await _uow.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    private Task<bool> FollowsAsync(string followerId, string targetId, CancellationToken ct)
        => _uow.Repository<Follow>()
               .AnyAsync(f => f.FollowerId == followerId && f.FollowingId == targetId, ct);
}
