using PersonaNest.Domain.Enums;

namespace PersonaNest.Services.DTOs.Responses;

/// <summary>An entry card in a feed - profile, dashboard, media page, home activity.</summary>
public sealed record EntryCardDto
{
    public int Id { get; init; }

    public int MediaId { get; init; }
    public string MediaTitle { get; init; } = string.Empty;

    /// <summary>Personal cover when set, otherwise the shared official cover (§5).</summary>
    public string? CoverUrl { get; init; }

    public string CategoryName { get; init; } = string.Empty;
    public string CategoryColorToken { get; init; } = string.Empty;

    public decimal? Rating { get; init; }
    public string? Review { get; init; }
    public EntryStatus Status { get; init; }
    public Privacy Privacy { get; init; }

    public string AuthorId { get; init; } = string.Empty;
    public string AuthorUserName { get; init; } = string.Empty;
    public string AuthorDisplayName { get; init; } = string.Empty;
    public string? AuthorProfilePictureUrl { get; init; }

    public int LikeCount { get; init; }
    public int CommentCount { get; init; }

    public DateTime CreatedAt { get; init; }
}
