namespace PersonaNest.Domain.Entities;

/// <summary>
/// A named accent preset. PersonaNest has one fixed cinematic-dark base palette; a Theme
/// selects the accent pair applied to a user's profile. There is no light theme
/// (decision D-3). Seeded with the design system's eight swatches.
/// </summary>
public class Theme
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Accent colour, #rrggbb — rendered as CSS <c>--primary</c>.</summary>
    public string PrimaryHex { get; set; } = string.Empty;

    /// <summary>Darker companion, #rrggbb — rendered as CSS <c>--primary-dim</c>.</summary>
    public string PrimaryDimHex { get; set; } = string.Empty;

    /// <summary>Exactly one Theme row is the default.</summary>
    public bool IsDefault { get; set; }

    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
}
