namespace PersonaNest.Services.DTOs.Responses;

/// <summary>One external-API search hit, shaped for the Add Media auto-fill form (bonus: Consume
/// an External API). Shared by every category-specific search service (TMDB, RAWG, Jikan, Google
/// Books, MusicBrainz) so the Add Media UI has one result shape to render regardless of source.</summary>
public sealed record ExternalSearchResultDto
{
    public string ExternalId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Overview { get; init; }
    public string? ImageUrl { get; init; }
    public int? ReleaseYear { get; init; }
}
