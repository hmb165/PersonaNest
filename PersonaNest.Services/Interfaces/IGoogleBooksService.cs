using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.Interfaces;

/// <summary>Consumes the Google Books API for the Books category's Add Media auto-fill and the
/// Books-filtered site search (bonus: Consume an External API). Needs <c>GoogleBooks:ApiKey</c>
/// configured (free, from Google Cloud Console) - unauthenticated "volumes" search now carries a
/// 0 daily quota, so keyless calls are rejected outright.</summary>
public interface IGoogleBooksService
{
    Task<IReadOnlyList<ExternalSearchResultDto>> SearchAsync(
        string query, CancellationToken cancellationToken = default);
}
