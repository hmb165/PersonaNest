using PersonaNest.Domain.Abstractions;
using PersonaNest.Services.Common;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.Interfaces;

/// <summary>The community media catalogue (§4).</summary>
public interface IMediaService
{
    Task<PagedResult<MediaCardDto>> SearchAsync(
        MediaSearchRequest request, CancellationToken cancellationToken = default);

    Task<MediaDetailDto?> GetDetailsAsync(
        int mediaId, string? viewerId, CancellationToken cancellationToken = default);

    /// <summary>Most-logged media, for the home page's "Trending this week" rail.</summary>
    Task<IReadOnlyList<MediaCardDto>> GetTrendingAsync(
        int count = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Candidates the user should look at before adding a new row. §4 asks to avoid duplicates
    /// "as much as reasonably possible", so this warns rather than blocks.
    /// </summary>
    Task<IReadOnlyList<MediaCardDto>> FindPossibleDuplicatesAsync(
        string title, int categoryId, int? releaseYear,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a media item. Fails when duplicates exist and the request has not set
    /// <c>ConfirmedNotDuplicate</c>.
    /// </summary>
    Task<ServiceResult<int>> CreateAsync(
        CreateMediaRequest request, string userId, CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdateAsync(
        UpdateMediaRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes and marks the row Rejected. Never a hard delete - Media -&gt; Entry is
    /// Restrict precisely so one removal cannot destroy every user's entries (decision D-9).
    /// </summary>
    Task<ServiceResult> SoftDeleteAsync(
        int mediaId, string moderatorId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default);
}
