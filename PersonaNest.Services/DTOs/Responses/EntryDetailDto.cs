using PersonaNest.Domain.Enums;

namespace PersonaNest.Services.DTOs.Responses;

/// <summary>The full entry page: review, favourite moment, tags, author, media context.</summary>
public sealed record EntryDetailDto
{
    public int Id { get; init; }

    public int MediaId { get; init; }
    public string MediaTitle { get; init; } = string.Empty;
    public string? MediaOfficialCoverUrl { get; init; }
    public int? MediaReleaseYear { get; init; }
    public int MediaEntryCount { get; init; }

    public string CategoryName { get; init; } = string.Empty;
    public string CategoryColorToken { get; init; } = string.Empty;

    /// <summary>Never overwrites the official cover (§5).</summary>
    public string? PersonalCoverUrl { get; init; }

    public decimal? Rating { get; init; }
    public string? Review { get; init; }
    public string? FavoriteMoment { get; init; }
    public EntryStatus Status { get; init; }
    public Privacy Privacy { get; init; }
    public DateTime? ConsumedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    public string AuthorId { get; init; } = string.Empty;
    public string AuthorUserName { get; init; } = string.Empty;
    public string AuthorDisplayName { get; init; } = string.Empty;
    public string? AuthorProfilePictureUrl { get; init; }

    public IReadOnlyList<TagDto> Tags { get; init; } = Array.Empty<TagDto>();

    public int LikeCount { get; init; }
    public int CommentCount { get; init; }

    /// <summary>True when the viewer owns this entry and may edit or delete it.</summary>
    public bool ViewerIsAuthor { get; init; }
}
