using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.Interfaces;

/// <summary>Consumes the RAWG Video Games Database API for the Games category's Add Media
/// auto-fill (bonus: Consume an External API). Same degrade-gracefully-with-no-key pattern as
/// <see cref="ITmdbService"/>.</summary>
public interface IRawgService
{
    Task<IReadOnlyList<ExternalSearchResultDto>> SearchAsync(
        string query, CancellationToken cancellationToken = default);
}
