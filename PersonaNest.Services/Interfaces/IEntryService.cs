using PersonaNest.Domain.Abstractions;
using PersonaNest.Services.Common;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.Interfaces;

/// <summary>
/// Entries - a user's experience of a media item (§5).
/// <para>
/// Privacy (§18) is enforced by the repository so it can be evaluated in SQL; this service
/// consumes those methods and never composes the visibility expression itself.
/// </para>
/// </summary>
public interface IEntryService
{
    Task<PagedResult<EntryCardDto>> GetForProfileAsync(
        string profileUserId, string? viewerId,
        int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    Task<PagedResult<EntrySummaryDto>> GetMineAsync(
        string userId, MyEntriesRequest request, CancellationToken cancellationToken = default);

    Task<EntryDetailDto?> GetDetailsAsync(
        int entryId, string? viewerId, CancellationToken cancellationToken = default);

    /// <summary>Community entries on a media page, filtered to what the viewer may see.</summary>
    Task<PagedResult<EntryCardDto>> GetForMediaAsync(
        int mediaId, string? viewerId,
        int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EntryCardDto>> GetFollowingFeedAsync(
        string viewerId, int page = 1, int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The viewer's existing entry for this media, if any. Lets the create flow redirect to Edit
    /// rather than hit the unique (UserId, MediaId) index (decision D-11).
    /// </summary>
    Task<int?> FindExistingEntryIdAsync(
        string userId, int mediaId, CancellationToken cancellationToken = default);

    Task<ServiceResult<int>> CreateAsync(
        CreateEntryRequest request, string userId, CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdateAsync(
        UpdateEntryRequest request, string userId, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes the entry and refreshes the media's cached aggregates.</summary>
    Task<ServiceResult> DeleteAsync(
        int entryId, string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TagDto>> GetTagsAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds or removes the viewer's like on an entry (§18). Returns the state after the toggle.</summary>
    Task<ServiceResult<bool>> ToggleLikeAsync(
        string userId, int entryId, CancellationToken cancellationToken = default);

    /// <summary>Entries the user created on or after <paramref name="since"/> - the Dashboard's "this week" stat.</summary>
    Task<int> CountCreatedSinceAsync(
        string userId, DateTime since, CancellationToken cancellationToken = default);
}
