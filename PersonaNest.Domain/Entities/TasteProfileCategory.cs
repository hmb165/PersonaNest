namespace PersonaNest.Domain.Entities;

/// <summary>One bar of the taste-profile category distribution (e.g. "Movies 38%").</summary>
public class TasteProfileCategory
{
    public int Id { get; set; }

    public string TasteProfileId { get; set; } = string.Empty;
    public TasteProfile TasteProfile { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public int EntryCount { get; set; }

    /// <summary>Share of the user's entries in this category, 0.00–100.00.</summary>
    public decimal Percentage { get; set; }
}
