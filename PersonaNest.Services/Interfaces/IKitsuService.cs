using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.Interfaces;

/// <summary>Consumes the Kitsu API for the Anime and Manga categories' Add Media auto-fill and
/// Anime-filtered site search (bonus: Consume an External API). No API key required. Replaces the
/// earlier Jikan (unofficial MyAnimeList) integration, which proved too unreliable in practice -
/// Jikan proxies MyAnimeList itself and returned frequent 429/504s even outside any rate limit,
/// including "MyAnimeList may be down/unavailable" errors from Jikan's own upstream.</summary>
public interface IKitsuService
{
    /// <param name="mediaType">"anime" or "manga".</param>
    Task<IReadOnlyList<ExternalSearchResultDto>> SearchAsync(
        string query, string mediaType, CancellationToken cancellationToken = default);
}
