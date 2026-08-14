namespace PersonaNest.Domain.Entities;

/// <summary>
/// A like on an Entry. Kept separate from <see cref="CommentLike"/> so both carry a real
/// foreign key to their target (decision D-2). Unique on (EntryId, UserId).
/// </summary>
public class EntryLike
{
    public int Id { get; set; }

    public int EntryId { get; set; }
    public Entry Entry { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
