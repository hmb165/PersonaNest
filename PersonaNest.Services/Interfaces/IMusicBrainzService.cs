using PersonaNest.Services.DTOs.Responses;

namespace PersonaNest.Services.Interfaces;

/// <summary>Consumes the MusicBrainz API for the Music category's Add Media auto-fill (bonus:
/// Consume an External API). No API key required, but MusicBrainz's usage policy requires a
/// descriptive User-Agent on every request.</summary>
public interface IMusicBrainzService
{
    Task<IReadOnlyList<ExternalSearchResultDto>> SearchAsync(
        string query, CancellationToken cancellationToken = default);
}
