using PersonaNest.Domain.Abstractions;
using PersonaNest.Services.Common;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.Interfaces;

/// <summary>Curated lists of media (§20).</summary>
public interface ICollectionService
{
    Task<PagedResult<CollectionCardDto>> GetForUserAsync(
        string ownerId, string? viewerId,
        int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    Task<CollectionDetailDto?> GetDetailsAsync(
        int collectionId, string? viewerId, CancellationToken cancellationToken = default);

    Task<ServiceResult<int>> CreateAsync(
        CreateCollectionRequest request, string userId, CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdateAsync(
        UpdateCollectionRequest request, string userId, CancellationToken cancellationToken = default);

    Task<ServiceResult> DeleteAsync(
        int collectionId, string userId, CancellationToken cancellationToken = default);

    Task<ServiceResult> AddItemAsync(
        AddCollectionItemRequest request, string userId, CancellationToken cancellationToken = default);

    Task<ServiceResult> RemoveItemAsync(
        int collectionId, int mediaId, string userId, CancellationToken cancellationToken = default);
}
