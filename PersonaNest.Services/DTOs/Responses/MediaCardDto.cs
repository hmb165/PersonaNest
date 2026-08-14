namespace PersonaNest.Services.DTOs.Responses;

/// <summary>A poster card in a grid - home, search results, favourites, collections.</summary>
public sealed record MediaCardDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? OfficialCoverUrl { get; init; }
    public string? Creator { get; init; }
    public int? ReleaseYear { get; init; }

    public string CategoryName { get; init; } = string.Empty;
    public string CategoryColorToken { get; init; } = string.Empty;

    /// <summary>Average across public entries only (decision D-16). Null when unrated.</summary>
    public decimal? AverageRating { get; init; }

    public int EntryCount { get; init; }
}
