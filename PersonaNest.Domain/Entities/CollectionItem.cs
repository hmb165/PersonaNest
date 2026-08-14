namespace PersonaNest.Domain.Entities;

/// <summary>
/// A Media item inside a Collection. Collections hold <em>media</em>, not entries — collection
/// cover art is drawn from the first four items' official covers.
/// </summary>
public class CollectionItem
{
    public int CollectionId { get; set; }
    public Collection Collection { get; set; } = null!;

    public int MediaId { get; set; }
    public Media Media { get; set; } = null!;

    public DateTime AddedAt { get; set; }

    /// <summary>Manual sort order within the collection.</summary>
    public int Position { get; set; }
}
