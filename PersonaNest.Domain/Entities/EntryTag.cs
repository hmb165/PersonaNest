namespace PersonaNest.Domain.Entities;

/// <summary>Join table for the Entry &lt;-&gt; Tag many-to-many (§21). Composite key.</summary>
public class EntryTag
{
    public int EntryId { get; set; }
    public Entry Entry { get; set; } = null!;

    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
