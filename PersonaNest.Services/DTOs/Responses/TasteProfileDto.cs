namespace PersonaNest.Services.DTOs.Responses;

/// <summary>
/// Pre-computed taste summary (§22). Read only - written by the Phase 12 background service.
/// </summary>
public sealed record TasteProfileDto
{
    public decimal? AverageRating { get; init; }
    public int TotalEntries { get; init; }
    public int TotalReviews { get; init; }
    public DateTime? MostActiveMonth { get; init; }
    public DateTime ComputedAt { get; init; }

    public IReadOnlyList<TasteCategorySliceDto> Categories { get; init; }
        = Array.Empty<TasteCategorySliceDto>();

    public IReadOnlyList<TasteTagDto> TopTags { get; init; } = Array.Empty<TasteTagDto>();
}

/// <summary>One bar of the category distribution, e.g. "Movies 38%".</summary>
public sealed record TasteCategorySliceDto
{
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string CategoryColorToken { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public int EntryCount { get; init; }
    public decimal Percentage { get; init; }
}

/// <summary>One row of the "Top Tags" panel.</summary>
public sealed record TasteTagDto
{
    public int TagId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int UseCount { get; init; }
}
