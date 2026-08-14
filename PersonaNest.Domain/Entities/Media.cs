using PersonaNest.Domain.Enums;

namespace PersonaNest.Domain.Entities;

/// <summary>
/// One shared, community-built catalogue row. There are deliberately no per-category tables
/// (§3); category-specific detail lives in the nullable columns below.
/// </summary>
public class Media
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Shared cover. An Entry's PersonalCoverUrl never overwrites this (§5).</summary>
    public string? OfficialCoverUrl { get; set; }

    /// <summary>Director / author / studio / artist — collected by the Add Media form.</summary>
    public string? Creator { get; set; }

    /// <summary>Year only: the Add Media form collects a year and the UI displays a year.</summary>
    public int? ReleaseYear { get; set; }

    public int? RuntimeMinutes { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string CreatedById { get; set; } = string.Empty;
    public ApplicationUser CreatedBy { get; set; } = null!;

    /// <summary>Defaults to Approved so §4's add-then-log flow is never blocked.</summary>
    public MediaStatus Status { get; set; } = MediaStatus.Approved;

    // ── Cached aggregates (decision D-20). Public entries only (decision D-16).
    // Written through on Entry create/edit/delete/privacy-change and reconciled nightly
    // by the background service.
    public decimal? AverageRating { get; set; }
    public int RatingCount { get; set; }
    public int EntryCount { get; set; }

    // ── Soft delete (decision D-9). Media -> Entry is Restrict, so media is never hard-deleted.
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedById { get; set; }
    public ApplicationUser? DeletedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Entry> Entries { get; set; } = new List<Entry>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<CollectionItem> CollectionItems { get; set; } = new List<CollectionItem>();
    public ICollection<MediaReport> Reports { get; set; } = new List<MediaReport>();
}
