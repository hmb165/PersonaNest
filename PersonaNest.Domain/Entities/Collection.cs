using PersonaNest.Domain.Enums;

namespace PersonaNest.Domain.Entities;

/// <summary>A user-curated list of Media items, e.g. "Comfort Movies" (§20).</summary>
public class Collection
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Privacy Privacy { get; set; } = Privacy.Public;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<CollectionItem> Items { get; set; } = new List<CollectionItem>();
}
