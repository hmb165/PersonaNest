namespace PersonaNest.Domain.Entities;

/// <summary>
/// A top-level media category (Games, Movies, TV Shows, Anime, Manga, Books, Music).
/// Seeded reference data, editable through Admin -> Category Management (§15). Icon, Slug and
/// ColorToken exist so the home pills, search filters and category badges are data-driven
/// rather than hard-coded in views (decision D-18).
/// </summary>
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Emoji shown on the home category pills.</summary>
    public string? Icon { get; set; }

    /// <summary>URL segment used by <c>/Search?category=movies</c>.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Design-system colour token: primary | success | accent | warning | info.</summary>
    public string ColorToken { get; set; } = string.Empty;

    public ICollection<Media> Media { get; set; } = new List<Media>();
    public ICollection<TasteProfileCategory> TasteProfileCategories { get; set; }
        = new List<TasteProfileCategory>();
}
