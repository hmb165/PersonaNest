using PersonaNest.Domain.Enums;

namespace PersonaNest.Domain.Entities;

/// <summary>
/// One user's experience of one Media item. Unique on (UserId, MediaId) — one entry per user
/// per media, no rewatch rows (decision D-11).
/// </summary>
public class Entry
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int MediaId { get; set; }
    public Media Media { get; set; } = null!;

    /// <summary>
    /// 0.5–10.0 in 0.5 increments, or null for an unrated entry. Rendered on the five-star
    /// widget where one star = 2.0 points (decision D-7).
    /// </summary>
    public decimal? Rating { get; set; }

    public string? Review { get; set; }
    public string? FavoriteMoment { get; set; }

    public EntryStatus Status { get; set; } = EntryStatus.Completed;
    public Privacy Privacy { get; set; } = Privacy.Public;

    /// <summary>Optional personal cover; falls back to Media.OfficialCoverUrl in views (§5).</summary>
    public string? PersonalCoverUrl { get; set; }

    /// <summary>When the user actually watched / read / played it.</summary>
    public DateTime? ConsumedAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedById { get; set; }
    public ApplicationUser? DeletedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<EntryLike> Likes { get; set; } = new List<EntryLike>();
    public ICollection<EntryTag> EntryTags { get; set; } = new List<EntryTag>();
    public ICollection<EntryReport> Reports { get; set; } = new List<EntryReport>();
}
