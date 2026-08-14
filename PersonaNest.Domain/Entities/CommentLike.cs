namespace PersonaNest.Domain.Entities;

/// <summary>A like on a Comment. Unique on (CommentId, UserId). See <see cref="EntryLike"/>.</summary>
public class CommentLike
{
    public int Id { get; set; }

    public int CommentId { get; set; }
    public Comment Comment { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
