using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.Interfaces;

/// <summary>
/// Consumes The Movie Database (TMDB) API so the Add Media form can auto-fill Movies/TV Shows
/// entries instead of manual typing. Read-only, no API key means "feature quietly unavailable"
/// (returns an empty list) rather than a broken page - matches the project's existing "missing
/// dev config degrades gracefully" pattern (e.g. <c>DevelopmentEmailSender</c>).
/// </summary>
public interface ITmdbService
{
    /// <param name="mediaType">TMDB's own vocabulary: "movie" or "tv".</param>
    Task<IReadOnlyList<ExternalSearchResultDto>> SearchAsync(
        string query, string mediaType, CancellationToken cancellationToken = default);
}
