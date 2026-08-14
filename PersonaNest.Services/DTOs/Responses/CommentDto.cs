namespace PersonaNest.Services.DTOs.Responses;

/// <summary>
/// A comment on an Entry, with at most one level of replies (§5, decision D-17 - the design
/// shows a Reply affordance but unbounded nesting is out of scope).
/// </summary>
public sealed record CommentDto
{
    public int Id { get; init; }
    public int EntryId { get; init; }
    public int? ParentCommentId { get; init; }
    public string Content { get; init; } = string.Empty;

    public string AuthorId { get; init; } = string.Empty;
    public string AuthorUserName { get; init; } = string.Empty;
    public string AuthorDisplayName { get; init; } = string.Empty;
    public string? AuthorProfilePictureUrl { get; init; }

    public int LikeCount { get; init; }
    public bool ViewerHasLiked { get; init; }
    public bool ViewerIsAuthor { get; init; }

    public DateTime CreatedAt { get; init; }

    /// <summary>Populated only on top-level comments; always empty on a reply.</summary>
    public IReadOnlyList<CommentDto> Replies { get; init; } = Array.Empty<CommentDto>();
}
