namespace PersonaNest.Domain.Entities;

/// <summary>
/// A label a user attaches to their own Entry (§21). <see cref="NormalizedName"/> carries the
/// unique index so "SciFi", "sci-fi " and "Sci-Fi" cannot become three tags.
/// </summary>
public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;

    public ICollection<EntryTag> EntryTags { get; set; } = new List<EntryTag>();
    public ICollection<TasteProfileTag> TasteProfileTags { get; set; } = new List<TasteProfileTag>();
}
