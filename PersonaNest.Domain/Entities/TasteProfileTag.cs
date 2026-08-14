namespace PersonaNest.Domain.Entities;

/// <summary>One row of the taste-profile "Top Tags" panel.</summary>
public class TasteProfileTag
{
    public int Id { get; set; }

    public string TasteProfileId { get; set; } = string.Empty;
    public TasteProfile TasteProfile { get; set; } = null!;

    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;

    public int UseCount { get; set; }
}
