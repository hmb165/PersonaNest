using PersonaNest.Domain.Abstractions;
using PersonaNest.Services.Common;
using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.Interfaces;

/// <summary>Favourite media (§19). Users favourite <em>media</em>; they like other people's entries.</summary>
public interface IFavoriteService
{
    Task<PagedResult<MediaCardDto>> GetForUserAsync(
        string userId, int page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<bool> IsFavoritedAsync(
        string userId, int mediaId, CancellationToken cancellationToken = default);

    /// <summary>Adds or removes. The value is the state <em>after</em> the toggle.</summary>
    Task<ServiceResult<bool>> ToggleAsync(
        string userId, int mediaId, CancellationToken cancellationToken = default);
}
