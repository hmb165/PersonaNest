using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.Interfaces;

/// <summary>Consumes the Google Books API for the Books category's Add Media auto-fill (bonus:
/// Consume an External API). No API key required - Google Books' public volumes search works
/// keyless at a lower rate limit, sufficient for interactive form-filling.</summary>
public interface IGoogleBooksService
{
    Task<IReadOnlyList<ExternalSearchResultDto>> SearchAsync(
        string query, CancellationToken cancellationToken = default);
}
