namespace PersonaNest.Domain.Entities;

/// <summary>
/// A comment on an Entry. Supports exactly one level of replies via
/// <see cref="ParentCommentId"/> (decision D-17) — the design shows a Reply affordance but
/// unbounded nesting is out of scope.
/// </summary>
public class Comment
{
    public int Id { get; set; }

    public int EntryId { get; set; }
    public Entry Entry { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    /// <summary>Null for a top-level comment. Replies to a reply are not permitted.</summary>
    public int? ParentCommentId { get; set; }
    public Comment? ParentComment { get; set; }
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();

    public string Content { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedById { get; set; }
    public ApplicationUser? DeletedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<CommentLike> Likes { get; set; } = new List<CommentLike>();
    public ICollection<CommentReport> Reports { get; set; } = new List<CommentReport>();
}
