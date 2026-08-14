namespace PersonaNest.Services.DTOs.Responses;

/// <summary>The shared community page for one media item.</summary>
public sealed record MediaDetailDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? OfficialCoverUrl { get; init; }
    public string? Creator { get; init; }
    public int? ReleaseYear { get; init; }
    public int? RuntimeMinutes { get; init; }

    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string CategoryColorToken { get; init; } = string.Empty;

    public decimal? AverageRating { get; init; }
    public int RatingCount { get; init; }
    public int EntryCount { get; init; }

    public string AddedByUserName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }

    // ── Viewer-relative state, null/false for anonymous visitors.
    public bool IsFavoritedByViewer { get; init; }

    /// <summary>The viewer's own entry for this media, if they have one (decision D-11).</summary>
    public int? ViewerEntryId { get; init; }
}
