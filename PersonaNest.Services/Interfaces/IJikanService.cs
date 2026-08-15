using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.Interfaces;

/// <summary>Consumes the Jikan (unofficial MyAnimeList) API for the Anime and Manga categories'
/// Add Media auto-fill (bonus: Consume an External API). No API key required.</summary>
public interface IJikanService
{
    /// <param name="mediaType">"anime" or "manga".</param>
    Task<IReadOnlyList<ExternalSearchResultDto>> SearchAsync(
        string query, string mediaType, CancellationToken cancellationToken = default);
}
